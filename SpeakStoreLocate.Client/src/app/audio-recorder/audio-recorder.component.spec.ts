import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AudioRecorderComponent } from './audio-recorder.component';

describe('AudioRecorderComponent', () => {
  let component: AudioRecorderComponent;
  let fixture: ComponentFixture<AudioRecorderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AudioRecorderComponent],
      imports: [HttpClientTestingModule],
      schemas: [NO_ERRORS_SCHEMA],
    })
    .compileComponents();

    fixture = TestBed.createComponent(AudioRecorderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Audio Level Indicator', () => {
    it('should initialize audioLevel to 0', () => {
      expect(component.audioLevel).toBe(0);
    });

    it('should reset audioLevel to 0 when stopAudioLevelMonitoring is called', () => {
      // Set a non-zero value first
      component.audioLevel = 50;
      // Call the private method using bracket notation
      (component as any).stopAudioLevelMonitoring();
      expect(component.audioLevel).toBe(0);
    });

    it('should not have isRecording true initially', () => {
      expect(component.isRecording).toBeFalse();
    });
  });
});
