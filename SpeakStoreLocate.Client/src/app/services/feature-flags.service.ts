import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly localSaveInsteadOfBackendKey = 'audioRecorder.localSaveInsteadOfBackend';

  private readonly _localSaveInsteadOfBackend$ = new BehaviorSubject<boolean>(this.readBool(this.localSaveInsteadOfBackendKey, false));

  readonly localSaveInsteadOfBackend$ = this._localSaveInsteadOfBackend$.asObservable();

  get localSaveInsteadOfBackend(): boolean {
    return this._localSaveInsteadOfBackend$.value;
  }

  setLocalSaveInsteadOfBackend(value: boolean): void {
    this._localSaveInsteadOfBackend$.next(value);
    this.writeBool(this.localSaveInsteadOfBackendKey, value);
  }

  toggleLocalSaveInsteadOfBackend(): void {
    this.setLocalSaveInsteadOfBackend(!this.localSaveInsteadOfBackend);
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
