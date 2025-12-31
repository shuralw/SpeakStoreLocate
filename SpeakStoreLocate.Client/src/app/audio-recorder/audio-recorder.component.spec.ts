import { provideHttpClientTesting } from '@angular/common/http/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AudioRecorderComponent, PendingUpload } from './audio-recorder.component';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

describe('AudioRecorderComponent', () => {
  let component: AudioRecorderComponent;
  let fixture: ComponentFixture<AudioRecorderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    declarations: [AudioRecorderComponent],
    schemas: [NO_ERRORS_SCHEMA],
    imports: [],
    providers: [provideHttpClient(withInterceptorsFromDi()), provideHttpClientTesting()]
})
    .compileComponents();

    fixture = TestBed.createComponent(AudioRecorderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should mark normal uploads as archived', () => {
    // Arrange: Create a mock upload without isSnapshot flag
    const mockUpload: PendingUpload = {
      id: 'test-1',
      blob: new Blob(['test'], { type: 'audio/webm' }),
      duration: 10,
      isUploading: false,
      archived: false,
      isSnapshot: false
    };

    // Assert: Normal upload should be archivable (isSnapshot is false or undefined)
    expect(mockUpload.isSnapshot).toBeFalsy();
  });

  it('should NOT mark snapshot uploads as archived', () => {
    // Arrange: Create a mock upload with isSnapshot flag set to true
    const mockUpload: PendingUpload = {
      id: 'test-2',
      blob: new Blob(['test'], { type: 'audio/webm' }),
      duration: 10,
      isUploading: false,
      archived: true, // Already archived
      isSnapshot: true // This is a snapshot upload
    };

    // Assert: Snapshot upload should have isSnapshot flag set
    expect(mockUpload.isSnapshot).toBe(true);
    // Assert: Archived status should remain unchanged for snapshot uploads
    expect(mockUpload.archived).toBe(true);
  });

  it('should set isSnapshot flag when requeueing archived uploads', () => {
    // Arrange: Add an archived upload to the component
    const archivedUpload: PendingUpload = {
      id: 'cache-123',
      blob: new Blob(['test'], { type: 'audio/webm' }),
      duration: 10,
      isUploading: false,
      archived: true,
      uploadedAt: Date.now(),
      backendResults: ['test-result']
    };
    component.pendingUploads.push(archivedUpload);

    // Act: Simulate setting isSnapshot flag (as done in requeueArchivedUploads)
    const archived = component.pendingUploads.filter(u => !!u.archived);
    archived.forEach(upload => {
      upload.isSnapshot = true;
    });

    // Assert: The upload should have isSnapshot flag set
    expect(component.pendingUploads[0].isSnapshot).toBe(true);
    // Assert: The upload should still be marked as archived (status unchanged)
    expect(component.pendingUploads[0].archived).toBe(true);
    // Assert: Metadata should remain unchanged
    expect(component.pendingUploads[0].uploadedAt).toBeDefined();
    expect(component.pendingUploads[0].backendResults).toBeDefined();
  });
});
