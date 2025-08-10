import { Component, NgZone, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AudioCache } from './audio-cache';

export interface PeriodicElement {
  location: string;
  name: string;
}

export interface PendingUpload {
  id: string;
  blob: Blob;
  duration: number;
  isUploading: boolean;
  audioUrl?: string;
}

@Component({
  selector: 'app-audio-recorder',
  templateUrl: './audio-recorder.component.html',
  styleUrls: ['./audio-recorder.component.scss'],
})
export class AudioRecorderComponent implements OnInit {

  // private readonly API_BASE = 'https://mkxrivn8wy.eu-central-1.awsapprunner.com/api/storage';
  private readonly API_BASE = 'http://localhost:5471/api/storage';
  dataSource: PeriodicElement[] = [];
  displayedColumns = ['location', 'name'];

  isRecording = false;
  showPopup = false;
  isSuccess = false;

  popupMessage = '';

  private mediaRecorder?: MediaRecorder;
  private audioChunks: BlobPart[] = [];
  private stream?: MediaStream;

  pendingUploads: PendingUpload[] = [];
  currentlyPlaying?: string;

  constructor(private http: HttpClient, private zone: NgZone) { }

  async ngOnInit() {
    void this.loadCachedUploadsAsync();
    void this.loadTableAsync();
  }

  private async loadTableAsync(): Promise<void> {
    try {
      const items = await this.tryGetTableItems();
      this.zone.run(() => {
        this.dataSource = items;
      });
    } catch {
      this.zone.run(() => {
        this.dataSource = [];
      });
    }
  }

  private async loadCachedUploadsAsync(): Promise<void> {
    try {
      // 1) Retrieve all stored audios from the cache
      const cached = await AudioCache.listAll();
      // Calculate duration in parallel for faster UI rendering
      const withDurations = await Promise.all(
        cached.map(async item => ({
          item,
          duration: await this.getAudioDuration(item.blob)
        }))
      );

      for (const { item, duration } of withDurations) {
        // 2) Für jedes Audio einen PendingUpload bauen und anzeigen
        const upload: PendingUpload = this.buildPendingUpload(item.blob, duration, `cache-${item.id}`);
        this.zone.run(() => this.pendingUploads.push(upload));
        // 3) Upload starten, aber nicht auf Abschluss warten
        void this.uploadAudio(upload, true).catch((err) => { console.error('Upload error:', err); /* Upload-Fehler wird intern behandelt */ });
      }
    } catch (err) {
      console.error('Failed to load cached uploads:', err);
    }
  }

  async onPointerDown(evt: PointerEvent, btn: any) {
    evt.preventDefault();

    // immer frisch holen, um leere Blobs zu vermeiden
    this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });

    // Pointer Capture auf den Button
    btn.setPointerCapture(evt.pointerId);

    this.audioChunks = [];
    this.mediaRecorder = new MediaRecorder(this.stream);
    this.mediaRecorder.ondataavailable = e => this.audioChunks.push(e.data);
    this.mediaRecorder.onstop = () => this.onRecordingStop();
    this.mediaRecorder.start();

    this.zone.run(() => this.isRecording = true);
  }

  onPointerUp(evt: PointerEvent, btn: any) {
    // nur wenn wir aufnehmen
    if (!this.mediaRecorder || this.mediaRecorder.state !== 'recording') {
      return;
    }

    // Pointer Capture lösen
    btn.releasePointerCapture(evt.pointerId);

    this.mediaRecorder.stop();
    this.zone.run(() => this.isRecording = false);
  }

  onPointerCancel(evt: PointerEvent, btn: any) {
    if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
      // Pointer Capture lösen und Aufnahme sauber beenden,
      // damit onRecordingStop() den Blob bauen und den Upload anstoßen kann
      btn.releasePointerCapture(evt.pointerId);
      this.mediaRecorder.stop();
    }
    // audioChunks NICHT leeren – sie werden in onRecordingStop() benötigt
    this.zone.run(() => this.isRecording = false);
  }

  private async onRecordingStop() {
    // Blob bauen
    const blob = new Blob(this.audioChunks, { type: 'audio/webm' });

    // Mic freigeben
    this.stream?.getTracks().forEach(t => t.stop());
    this.stream = undefined;

    // Audio-Dauer ermitteln und PendingUpload-Objekt erstellen
    const duration = await this.getAudioDuration(blob);
    const upload = this.buildPendingUpload(blob, duration);

    this.zone.run(() => {
      this.pendingUploads.push(upload);
    });

    // Upload starten
    await this.uploadAudio(upload);
  }

  private async uploadAudio(upload: PendingUpload, isRetry = false): Promise<void> {

    const form = new FormData();
    form.append('audioFile', upload.blob, 'recording.webm');

    // Upload als "uploading" markieren
    this.zone.run(() => {
      upload.isUploading = true;
    });

    try {
      const results = await firstValueFrom(
        this.http.post<string[]>(`${this.API_BASE}/upload-audio`, form)
      );

      // Upload erfolgreich - aus Liste entfernen
      this.zone.run(() => {
        const index = this.pendingUploads.findIndex(p => p.id === upload.id);
        if (index >= 0) {
          // URL freigeben
          if (this.pendingUploads[index].audioUrl) {
            URL.revokeObjectURL(this.pendingUploads[index].audioUrl!);
          }
          this.pendingUploads.splice(index, 1);
        }
      });

      // Erfolgsmeldungen anzeigen
      for (const res of results) {
        this.showResult(true, `Erfolgreich gespeichert: ${res}`);
        await new Promise(resolve => setTimeout(resolve, 5500));
      }

      // Falls es sich um einen gecachten Eintrag handelt, aus IndexedDB entfernen
      if (upload.id.startsWith('cache-')) {
        const numericId = parseInt(upload.id.replace('cache-', ''), 10);
        if (!Number.isNaN(numericId)) {
          try { await AudioCache.remove(numericId); } catch { }
        }
      }

      // Tabelle aktualisieren
      this.dataSource = await this.tryGetTableItems();
    } catch {
      // Upload fehlgeschlagen
      this.zone.run(() => {
        upload.isUploading = false;
      });

      if (!isRetry) {
        await AudioCache.save(upload.blob);
        this.showResult(false, 'Offline: Audio wurde lokal gespeichert und wird später hochgeladen.');
      } else {
        this.showResult(false, 'Fehler beim Upload!');
      }
    }
  }

  // Baut ein PendingUpload-Objekt inklusive optionaler ID
  private buildPendingUpload(blob: Blob, duration: number, id: string = Date.now().toString()): PendingUpload {
    return {
      id,
      blob,
      duration,
      isUploading: false,
      audioUrl: URL.createObjectURL(blob)
    };
  }

  private async getAudioDuration(blob: Blob): Promise<number> {
    return new Promise((resolve) => {
      const audio = new Audio();
      audio.src = URL.createObjectURL(blob);
      audio.addEventListener('loadedmetadata', () => {
        URL.revokeObjectURL(audio.src);
        resolve(audio.duration);
      });
      audio.addEventListener('error', () => {
        URL.revokeObjectURL(audio.src);
        resolve(0);
      });
    });
  }

  playAudio(upload: PendingUpload) {
    if (this.currentlyPlaying === upload.id) {
      // Stop current audio
      this.currentlyPlaying = undefined;
      return;
    }

    if (upload.audioUrl) {
      const audio = new Audio(upload.audioUrl);
      this.currentlyPlaying = upload.id;

      audio.addEventListener('ended', () => {
        this.zone.run(() => {
          this.currentlyPlaying = undefined;
        });
      });

      audio.play();
    }
  }

  retryUpload(upload: PendingUpload) {
    if (!upload.isUploading) {
      this.uploadAudio(upload, true);
    }
  }

  removeUpload(upload: PendingUpload) {
    const index = this.pendingUploads.findIndex(p => p.id === upload.id);
    if (index >= 0) {
      if (upload.audioUrl) {
        URL.revokeObjectURL(upload.audioUrl);
      }
      this.pendingUploads.splice(index, 1);
    }
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  private showResult(success: boolean, message: string) {
    this.zone.run(() => {
      this.isSuccess = success;
      this.popupMessage = message;
      this.showPopup = true;
    });
    setTimeout(() => this.zone.run(() => this.showPopup = false), 5000);
  }

  private async tryGetTableItems(): Promise<PeriodicElement[]> {
    try {
      return firstValueFrom(
        this.http.get<PeriodicElement[]>(`${this.API_BASE}`)
      );
    } catch {
      this.showResult(false, 'Tabelle konnte nicht geladen werden.');
      return [];
    }
  }
}
