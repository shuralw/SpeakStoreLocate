import { HttpClient } from '@angular/common/http';
import { Component, NgZone, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface PeriodicElement {
  name: string;
  location: string;
}

const ELEMENT_DATA: PeriodicElement[] = [
  { location: 'Karton A', name: 'Diddle Notizbuch' },
  { location: 'Karton B', name: 'xD' },
];

@Component({
  selector: 'app-audio-recorder',
  templateUrl: './audio-recorder.component.html',
  styleUrls: ['./audio-recorder.component.scss'],
})
export class AudioRecorderComponent implements OnInit {

  private readonly API_BASE = 'https://mkxrivn8wy.eu-central-1.awsapprunner.com/api/storage';
  dataSource: PeriodicElement[] = [];
  isSuccess = false;
  popupMessage = '';
  showPopup = false;
  displayedColumns: string[] = ['location', 'name'];
  isRecording = false;

  private mediaRecorder!: MediaRecorder;
  private audioChunks: BlobPart[] = [];
  private stream!: MediaStream;

  constructor(private http: HttpClient, private zone: NgZone) {
  }

  async ngOnInit() {
    this.dataSource = await this.getTableItems();
  }

  async onPointerDown(evt: PointerEvent) {
    evt.preventDefault();
    if (!this.stream) {
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    }
    // Statt ViewChild: currentTarget ist hier das Button-Element
    const btn = evt.currentTarget as HTMLElement;
    btn.setPointerCapture(evt.pointerId);

    this.audioChunks = [];
    this.mediaRecorder = new MediaRecorder(this.stream);
    this.mediaRecorder.ondataavailable = e => this.audioChunks.push(e.data);
    this.mediaRecorder.onstop = () => this.onRecordingStop();
    this.mediaRecorder.start();

    this.zone.run(() => this.isRecording = true);
  }

  onPointerUp(evt: PointerEvent) {
    const btn = evt.currentTarget as HTMLElement;
    btn.releasePointerCapture(evt.pointerId);

    this.mediaRecorder.stop();
    this.zone.run(() => this.isRecording = false);
  }

  onPointerCancel(evt: PointerEvent) {
    const btn = evt.currentTarget as HTMLElement;
    btn.releasePointerCapture(evt.pointerId);

    if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
      this.mediaRecorder.stop();
    }
    this.audioChunks = [];
    this.zone.run(() => this.isRecording = false);
  }

  private async onRecordingStop() {
    const blob = new Blob(this.audioChunks, { type: 'audio/webm' });
    this.stream.getTracks().forEach(t => t.stop());
    await this.uploadAudio(blob);
  }

  private async uploadAudio(blob: Blob) {
    const form = new FormData();
    form.append('audioFile', blob, 'recording.webm');
    try {
      await firstValueFrom(
        this.http.post(`${this.API_BASE}/upload-audio`, form, { responseType: 'text' })
      );
      this.showResult(true, 'Erfolgreich gespeichert!');
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

  private async getTableItems(): Promise<any[]> {
    return await firstValueFrom(
      this.http.get<any[]>(`${this.API_BASE}`)
    );
  }
}