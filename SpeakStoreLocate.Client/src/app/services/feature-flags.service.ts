import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly localSaveInsteadOfBackendKey = 'audioRecorder.localSaveInsteadOfBackend';
  private readonly showArchivedUploadsUiKey = 'audioRecorder.showArchivedUploadsUi';
  private readonly enableArchivedDownloadsKey = 'audioRecorder.enableArchivedDownloads';
  private readonly enableArchivedRequeueKey = 'audioRecorder.enableArchivedRequeue';

  private readonly _localSaveInsteadOfBackend$ = new BehaviorSubject<boolean>(this.readBool(this.localSaveInsteadOfBackendKey, false));
  private readonly _showArchivedUploadsUi$ = new BehaviorSubject<boolean>(this.readBool(this.showArchivedUploadsUiKey, false));
  private readonly _enableArchivedDownloads$ = new BehaviorSubject<boolean>(this.readBool(this.enableArchivedDownloadsKey, true));
  private readonly _enableArchivedRequeue$ = new BehaviorSubject<boolean>(this.readBool(this.enableArchivedRequeueKey, true));

  readonly localSaveInsteadOfBackend$ = this._localSaveInsteadOfBackend$.asObservable();
  readonly showArchivedUploadsUi$ = this._showArchivedUploadsUi$.asObservable();
  readonly enableArchivedDownloads$ = this._enableArchivedDownloads$.asObservable();
  readonly enableArchivedRequeue$ = this._enableArchivedRequeue$.asObservable();

  get localSaveInsteadOfBackend(): boolean {
    return this._localSaveInsteadOfBackend$.value;
  }

  get showArchivedUploadsUi(): boolean {
    return this._showArchivedUploadsUi$.value;
  }

  get enableArchivedDownloads(): boolean {
    return this._enableArchivedDownloads$.value;
  }

  get enableArchivedRequeue(): boolean {
    return this._enableArchivedRequeue$.value;
  }

  setLocalSaveInsteadOfBackend(value: boolean): void {
    this._localSaveInsteadOfBackend$.next(value);
    this.writeBool(this.localSaveInsteadOfBackendKey, value);
  }

  setShowArchivedUploadsUi(value: boolean): void {
    this._showArchivedUploadsUi$.next(value);
    this.writeBool(this.showArchivedUploadsUiKey, value);
  }

  setEnableArchivedDownloads(value: boolean): void {
    this._enableArchivedDownloads$.next(value);
    this.writeBool(this.enableArchivedDownloadsKey, value);
  }

  setEnableArchivedRequeue(value: boolean): void {
    this._enableArchivedRequeue$.next(value);
    this.writeBool(this.enableArchivedRequeueKey, value);
  }

  toggleLocalSaveInsteadOfBackend(): void {
    this.setLocalSaveInsteadOfBackend(!this.localSaveInsteadOfBackend);
  }

  toggleShowArchivedUploadsUi(): void {
    this.setShowArchivedUploadsUi(!this.showArchivedUploadsUi);
  }

  toggleEnableArchivedDownloads(): void {
    this.setEnableArchivedDownloads(!this.enableArchivedDownloads);
  }

  toggleEnableArchivedRequeue(): void {
    this.setEnableArchivedRequeue(!this.enableArchivedRequeue);
  }

  private readBool(key: string, defaultValue: boolean): boolean {
    try {
      const persisted = localStorage.getItem(key);
      if (persisted === null) return defaultValue;
      return persisted === 'true';
    } catch {
      return defaultValue;
    }
  }

  private writeBool(key: string, value: boolean): void {
    try {
      localStorage.setItem(key, value ? 'true' : 'false');
    } catch {
      // ignore
    }
  }
}
