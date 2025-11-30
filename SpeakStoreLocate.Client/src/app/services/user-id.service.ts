import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

export const USER_ID_REGEX = /^[A-Za-z0-9_-]{1,64}$/;

export interface IdentityProvider {
  getUserId(): string | null;
  setUserId(id: string | null): void;
}

@Injectable({ providedIn: 'root' })
export class LocalHeaderUserIdProvider implements IdentityProvider {
  private readonly storageKey = 'userId';

  getUserId(): string | null {
    const value = localStorage.getItem(this.storageKey);
    return value ? value : null;
  }

  setUserId(id: string | null): void {
    if (!id) {
      localStorage.removeItem(this.storageKey);
      return;
    }

    localStorage.setItem(this.storageKey, id);
  }
}

@Injectable({ providedIn: 'root' })
export class UserIdService {
  private provider: IdentityProvider;
  readonly changes$ = new BehaviorSubject<string | null>(null);
  private readonly focusRequestsSubject = new Subject<void>();
  readonly focusRequests$ = this.focusRequestsSubject.asObservable();

  constructor(localProvider: LocalHeaderUserIdProvider) {
    // Default provider can later be swapped for a JWT based identity source.
    this.provider = localProvider;
    this.changes$.next(this.provider.getUserId());
  }

  getUserId(): string | null {
    const userId = this.provider.getUserId();
    return this.isValidUserId(userId) ? userId : null;
  }

  setUserId(id: string | null): void {
    const trimmed = id ? id.trim() : '';
    if (!trimmed) {
      this.provider.setUserId(null);
      this.changes$.next(null);
      return;
    }

    if (!this.isValidUserId(trimmed)) {
      throw new Error('Invalid user identifier');
    }

    this.provider.setUserId(trimmed);
    this.changes$.next(trimmed);
  }

  isValidUserId(id: string | null | undefined): boolean {
    if (!id) {
      return false;
    }

    return USER_ID_REGEX.test(id);
  }

  requestFocus(): void {
    this.focusRequestsSubject.next();
  }

  setProvider(provider: IdentityProvider): void {
    this.provider = provider;
    this.changes$.next(this.provider.getUserId());
  }
}
