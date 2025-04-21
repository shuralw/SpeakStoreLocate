import { Component, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-audio-recorder',
  templateUrl: './audio-recorder.component.html',
  styleUrls: ['./audio-recorder.component.scss']
})
export class AudioRecorderComponent {
  isRecording   = false;
  showPopup     = false;
  isSuccess     = false;
  popupMessage  = '';

  private mediaRecorder!: MediaRecorder;
  private chunks: Blob[] = [];

  constructor(private http: HttpClient, private zone: NgZone) {}

  startRecording(): void {
    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        this.chunks = [];
        this.mediaRecorder = new MediaRecorder(stream);
        this.mediaRecorder.ondataavailable = e => {
          if (e.data.size > 0) this.chunks.push(e.data);
        };
        this.mediaRecorder.start();
        this.zone.run(() => this.isRecording = true);
      })
      .catch(err => {
        console.error('getUserMedia-Fehler:', err);
        this.showResult(false, 'Konnte Mikrofon nicht öffnen.');
      });
  }

  stopRecording(): void {
    if (!this.mediaRecorder) return;
    this.mediaRecorder.onstop = () => {
      const blob = new Blob(this.chunks, { type: 'audio/webm' });
      this.uploadAudio(blob);
    };
    this.mediaRecorder.stop();
    this.zone.run(() => this.isRecording = false);
  }

  private uploadAudio(blob: Blob): void {
    const form = new FormData();
    form.append('audioFile', blob, 'recording.webm');

    this.http.post('https://mkxrivn8wy.eu-central-1.awsapprunner.com/api/storage/upload-audio', form, { responseType: 'text' })
      .subscribe({
        next: () => this.showResult(true, 'Erfolgreich gespeichert!'),
        error: () => this.showResult(false, 'Fehler beim Speichern!')
      });
  }

  private showResult(success: boolean, message: string): void {
    this.zone.run(() => {
      this.isSuccess    = success;
      this.popupMessage = message;
      this.showPopup    = true;
    });
    setTimeout(() => this.zone.run(() => this.showPopup = false), 5000);
  }
}
