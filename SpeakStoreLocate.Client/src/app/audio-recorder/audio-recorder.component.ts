import { Component, NgZone, OnInit, ViewChild } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { AudioCache } from './audio-cache';
import { isUserIdHeaderError } from '../utils/http-error.utils';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { FormControl } from '@angular/forms';

export interface PeriodicElement {
  id: string;
  location: string;
  name: string;
}

export interface PendingUpload {
  id: string;
  blob: Blob;
  duration: number;
  isUploading: boolean;
  audioUrl?: string;
  archived?: boolean;
  uploadedAt?: number;
  backendResults?: string[];
}

@Component({
  selector: 'app-audio-recorder',
  templateUrl: './audio-recorder.component.html',
  styleUrls: ['./audio-recorder.component.scss'],
})
export class AudioRecorderComponent implements OnInit {

  // Base URL is provided via Angular environments for dev/prod
  private readonly API_BASE = environment.apiBase;
  dataSource = new MatTableDataSource<PeriodicElement>([]);
  displayedColumns = ['location', 'name', 'actions'];
  filterNameControl = new FormControl('');
  filterLocationControl = new FormControl('');
  @ViewChild(MatSort) sort?: MatSort;
  // Filters visibility toggle (default hidden)
  showFilters = false;

  isRecording = false;
  showPopup = false;
  isSuccess = false;

  popupMessage = '';

  private mediaRecorder?: MediaRecorder;
  private audioChunks: BlobPart[] = [];
  private stream?: MediaStream;

  pendingUploads: PendingUpload[] = [];
  currentlyPlaying?: string;

  /** Pending uploads only (archived uploads are kept for future features). */
  get pendingUploadsActive(): PendingUpload[] {
    return this.pendingUploads.filter(u => !u.archived);
  }

  /** Archived uploads count (kept locally after successful upload). */
  get archivedUploadsCount(): number {
    return this.pendingUploads.filter(u => !!u.archived).length;
  }

  // Editing state
  editingId?: string;
  editName: string = '';
  editLocation: string = '';
  private editingOriginalName: string = '';
  private editingOriginalLocation: string = '';

  constructor(private http: HttpClient, private zone: NgZone) { }

  async ngOnInit() {
    // Restore filter visibility preference
    try {
      const persisted = localStorage.getItem('audioRecorder.showFilters');
      this.showFilters = persisted === 'true';
    } catch {}
    void this.loadCachedUploadsAsync();
    void this.loadTableAsync();
  }

  private async loadTableAsync(): Promise<void> {
    try {
      const items = await this.tryGetTableItems();
      this.zone.run(() => {
        this.dataSource.data = items;
        if (this.sort) {
          this.dataSource.sort = this.sort;
          // Default sort by name ascending
          (this.sort as MatSort).active = 'name';
          (this.sort as MatSort).direction = 'asc';
        }
        this.setupFilters();
      });
    } catch {
      this.zone.run(() => {
        this.dataSource.data = [];
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
        upload.archived = item.status === 'archived';
        upload.uploadedAt = item.uploadedAt;
        upload.backendResults = item.backendResults;
        this.zone.run(() => this.pendingUploads.push(upload));

        // 3) Upload starten, aber nicht auf Abschluss warten (nur wenn nicht archiviert)
        if (!upload.archived) {
          void this.uploadAudio(upload, true).catch((err) => { console.error('Upload error:', err); /* Upload-Fehler wird intern behandelt */ });
        }
      }
    } catch (err) {
      console.error('Failed to load cached uploads:', err);
    }
  }

  async onPointerDown(evt: PointerEvent) {
    console.log('[AudioRecorderComponent] onPointerDown', evt);
    evt.preventDefault();

    // immer frisch holen, um leere Blobs zu vermeiden
    this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });

    // Pointer Capture auf den Button
    const btn = evt.target as HTMLElement;
    if (btn && typeof (btn as any).setPointerCapture === 'function') {
      (btn as any).setPointerCapture(evt.pointerId);
      console.debug('[AudioRecorderComponent] setPointerCapture called', btn, evt.pointerId);
    } else {
      console.warn('[AudioRecorderComponent] setPointerCapture not available on target:', btn);
    }

    this.audioChunks = [];
    this.mediaRecorder = new MediaRecorder(this.stream);
    this.mediaRecorder.ondataavailable = e => {
      console.log('[AudioRecorderComponent] ondataavailable', e);
      this.audioChunks.push(e.data);
    };
    this.mediaRecorder.onstop = () => {
      console.log('[AudioRecorderComponent] mediaRecorder.onstop');
      this.onRecordingStop();
    };
    this.mediaRecorder.start();
    console.log('[AudioRecorderComponent] mediaRecorder started');

    this.zone.run(() => this.isRecording = true);
  }

  onPointerUp(evt: PointerEvent) {
    console.log('[AudioRecorderComponent] onPointerUp', evt);
    // nur wenn wir aufnehmen
    if (!this.mediaRecorder || this.mediaRecorder.state !== 'recording') {
      return;
    }

    // Pointer Capture lösen
    const btn = evt.target as HTMLElement;
    if (btn && typeof (btn as any).releasePointerCapture === 'function') {
      (btn as any).releasePointerCapture(evt.pointerId);
      console.debug('[AudioRecorderComponent] releasePointerCapture called', btn, evt.pointerId);
    } else {
      console.warn('[AudioRecorderComponent] releasePointerCapture not available on target:', btn);
    }

    this.mediaRecorder.stop();
    console.log('[AudioRecorderComponent] mediaRecorder stopped');
    this.zone.run(() => this.isRecording = false);
  }

  onPointerCancel(evt: PointerEvent) {
    console.log('[AudioRecorderComponent] onPointerCancel', evt);
    if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
      // Pointer Capture lösen und Aufnahme sauber beenden,
      // damit onRecordingStop() den Blob bauen und den Upload anstoßen kann
      const btn = evt.target as HTMLElement;
      if (btn && typeof (btn as any).releasePointerCapture === 'function') {
        (btn as any).releasePointerCapture(evt.pointerId);
        console.debug('[AudioRecorderComponent] releasePointerCapture called (cancel)', btn, evt.pointerId);
      } else {
        console.warn('[AudioRecorderComponent] releasePointerCapture not available on target (cancel):', btn);
      }
      this.mediaRecorder.stop();
      console.log('[AudioRecorderComponent] mediaRecorder stopped (cancel)');
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

      // Upload erfolgreich - als archiviert markieren (nicht mehr pending)
      this.zone.run(() => {
        const index = this.pendingUploads.findIndex(p => p.id === upload.id);
        if (index >= 0) {
          this.pendingUploads[index].isUploading = false;
          this.pendingUploads[index].archived = true;
          this.pendingUploads[index].uploadedAt = Date.now();
          this.pendingUploads[index].backendResults = results;
        }
      });

      // Erfolgsmeldungen anzeigen
      for (const res of results) {
        this.showResult(true, `Erfolgreich gespeichert: ${res}`);
        await new Promise(resolve => setTimeout(resolve, 5500));
      }

      // Falls es sich um einen gecachten Eintrag handelt: als archiviert markieren (nicht löschen)
      if (upload.id.startsWith('cache-')) {
        const numericId = parseInt(upload.id.replace('cache-', ''), 10);
        if (!Number.isNaN(numericId)) {
          try {
            await AudioCache.markArchived(numericId, { uploadedAt: Date.now(), backendResults: results });
          } catch (err) {
            console.error('[AudioRecorderComponent] Failed to mark cached upload as archived:', err);
          }
        }
      }

      // Tabelle aktualisieren
      const items = await this.tryGetTableItems();
      this.zone.run(() => {
        this.dataSource.data = items;
      });
    } catch (err: any) {
      const httpError = err as HttpErrorResponse | undefined;
      // Upload fehlgeschlagen
      this.zone.run(() => {
        upload.isUploading = false;
      });

      if (httpError && isUserIdHeaderError(httpError)) {
        this.showResult(false, 'Upload fehlgeschlagen: Bitte User-ID setzen und erneut versuchen.');
        return;
      }

      const networkError = this.isNetworkError(err);

      if (!isRetry && networkError) {
        try {
          await AudioCache.save(upload.blob);
          console.info('[AudioRecorderComponent] Blob saved to AudioCache.');
        } catch (err) {
          console.error('[AudioRecorderComponent] Error saving blob to AudioCache:', err);
        }
        this.showResult(false, 'Offline: Audio wurde lokal gespeichert und wird später hochgeladen.');
      } else {
        this.showResult(false, 'Fehler beim Upload!');
      }
    }
  }

  private isNetworkError(err: any): boolean {
    if (err instanceof HttpErrorResponse) {
      return err.status === 0;
    }
    if (typeof navigator !== 'undefined' && navigator.onLine === false) {
      return true;
    }
    if (err && typeof err === 'object') {
      if (typeof err.status === 'number' && err.status === 0) return true;
      if (err.error && err.error instanceof ProgressEvent) return true;
    }
    return false;
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

      const cleanup = () => {
        URL.revokeObjectURL(audio.src);
        audio.remove();
      };

      const resolveIfReady = () => {
        if (isFinite(audio.duration) && !isNaN(audio.duration) && audio.duration > 0) {
          cleanup();
          resolve(audio.duration);
        }
      };

      audio.addEventListener('loadedmetadata', resolveIfReady);
      audio.addEventListener('durationchange', resolveIfReady);
      audio.addEventListener('error', () => {
        cleanup();
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
      const raw = await firstValueFrom(
        this.http.get<any[]>(`${this.API_BASE}`)
      );
      // Map API (Id/Name/Location) to client model (id/name/location)
      return (raw ?? []).map(x => ({
        id: x.id ?? x.Id,
        name: x.name ?? x.Name,
        location: x.location ?? x.Location,
      } as PeriodicElement));
    } catch {
      this.showResult(false, 'Tabelle konnte nicht geladen werden.');
      return [];
    }
  }

  private setupFilters() {
    // Column-specific filtering using two controls. We encode both values into one trigger string,
    // but actually read the controls inside the predicate.
    this.dataSource.filterPredicate = (data: PeriodicElement, _filter: string) => {
      const nameTerm = (this.filterNameControl.value ?? '').toString().trim().toLowerCase();
      const locTerm = (this.filterLocationControl.value ?? '').toString().trim().toLowerCase();

      const nameMatch = !nameTerm || (data.name ?? '').toLowerCase().includes(nameTerm);
      const locMatch = !locTerm || (data.location ?? '').toLowerCase().includes(locTerm);
      return nameMatch && locMatch;
    };

    const apply = () => {
      // Any change triggers filtering; value content is irrelevant due to predicate reading controls.
      this.dataSource.filter = `${Date.now()}`;
    };

    apply();
    this.filterNameControl.valueChanges.subscribe(() => apply());
    this.filterLocationControl.valueChanges.subscribe(() => apply());
  }

  toggleFilters() {
    this.showFilters = !this.showFilters;
    try { localStorage.setItem('audioRecorder.showFilters', this.showFilters ? 'true' : 'false'); } catch {}
  }

  startEdit(row: PeriodicElement) {
    // If already editing another row and unsaved changes exist -> confirm
    if (this.editingId && this.editingId !== row.id) {
      const hasUnsaved = (this.editName !== this.editingOriginalName) || (this.editLocation !== this.editingOriginalLocation);
      if (hasUnsaved) {
        const proceed = window.confirm('Es gibt ungespeicherte Änderungen. Wirklich wechseln und Änderungen verwerfen?');
        if (!proceed) {
          return; // abort switching
        }
      }
    }

    this.editingId = row.id;
    this.editName = row.name;
    this.editLocation = row.location;
    this.editingOriginalName = row.name;
    this.editingOriginalLocation = row.location;
  }

  cancelEdit() {
    this.editingId = undefined;
    this.editName = '';
    this.editLocation = '';
    this.editingOriginalName = '';
    this.editingOriginalLocation = '';
  }

  async saveEdit(row: PeriodicElement) {
    const trimmedName = (this.editName ?? '').trim();
    const trimmedLocation = (this.editLocation ?? '').trim();
    if (!trimmedName || !trimmedLocation) {
      this.showResult(false, 'Name und Ort dürfen nicht leer sein.');
      return;
    }

    try {
      const body = { name: trimmedName, location: trimmedLocation };
      await firstValueFrom(this.http.put(`${this.API_BASE}/${row.id}`, body));
      this.showResult(true, 'Eintrag aktualisiert.');
      this.cancelEdit();
      // After successful save, originals reset
      this.editingOriginalName = '';
      this.editingOriginalLocation = '';
      await this.loadTableAsync();
    } catch (err) {
      const httpError = err as HttpErrorResponse | undefined;
      if (httpError && isUserIdHeaderError(httpError)) {
        this.showResult(false, 'Aktualisierung fehlgeschlagen: Bitte User-ID setzen.');
        return;
      }
      this.showResult(false, 'Fehler beim Aktualisieren.');
    }
  }
}
