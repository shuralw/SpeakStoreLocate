import { Component, NgZone, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface PeriodicElement {
  location: string;
  name: string;
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

  constructor(private http: HttpClient, private zone: NgZone) {}

  async ngOnInit() {
    this.dataSource = await this.getTableItems();
  }

  async onPointerDown(evt: PointerEvent, btn: HTMLElement) {
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

  onPointerUp(evt: PointerEvent, btn: HTMLElement) {
    // nur wenn wir aufnehmen
    if (!this.mediaRecorder || this.mediaRecorder.state !== 'recording') {
      return;
    }

    // Pointer Capture lösen
    btn.releasePointerCapture(evt.pointerId);

    this.mediaRecorder.stop();
    this.zone.run(() => this.isRecording = false);
  }

  onPointerCancel(evt: PointerEvent, btn: HTMLElement) {
    if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
      btn.releasePointerCapture(evt.pointerId);
      this.mediaRecorder.stop();
    }
    this.audioChunks = [];
    this.zone.run(() => this.isRecording = false);
  }

  private async onRecordingStop() {
    // Blob bauen
    const blob = new Blob(this.audioChunks, { type: 'audio/webm' });

    // Mic freigeben
    this.stream?.getTracks().forEach(t => t.stop());
    this.stream = undefined;

    // Debug: console.log('Blob size:', blob.size);
    let result = await this.uploadAudio(blob);
  }

  private async uploadAudio(blob: Blob): Promise<void> {
    const form = new FormData();
    form.append('audioFile', blob, 'recording.webm');
  
    try {
      // Wir erwarten jetzt ein JSON-Array von Strings vom Server
      const results = await firstValueFrom(
        this.http.post<string[]>(`${this.API_BASE}/upload-audio`, form)
      );
  
      // Für jedes Resultat eine Meldung anzeigen
      for (const res of results) {
        this.showResult(true, `Erfolgreich gespeichert: ${res}`);
        // Warten bis das Popup wieder verschwindet, bevor wir das nächste anzeigen
        await new Promise(resolve => setTimeout(resolve, 5500));
      }
  
      // Tabelle anschließend einmalig aktualisieren
      this.dataSource = await this.getTableItems();
    } catch {
      this.showResult(false, 'Fehler beim Speichern!');
    }
  }
  

  private showResult(success: boolean, message: string) {
    this.zone.run(() => {
      this.isSuccess = success;
      this.popupMessage = message;
      this.showPopup = true;
    });
    setTimeout(() => this.zone.run(() => this.showPopup = false), 5000);
  }

  private async getTableItems(): Promise<PeriodicElement[]> {
    return firstValueFrom(
      this.http.get<PeriodicElement[]>(`${this.API_BASE}`)
    );
  }
}
