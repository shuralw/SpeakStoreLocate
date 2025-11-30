import { TestBed } from '@angular/core/testing';
import { IdentityProvider, LocalHeaderUserIdProvider, UserIdService } from './user-id.service';

class LocalHeaderUserIdProviderStub implements IdentityProvider {
  private value: string | null = null;

  getUserId(): string | null {
    return this.value;
  }

  setUserId(id: string | null): void {
    this.value = id;
  }
}

describe('UserIdService', () => {
  let service: UserIdService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        UserIdService,
        { provide: LocalHeaderUserIdProvider, useClass: LocalHeaderUserIdProviderStub },
      ],
    });
    service = TestBed.inject(UserIdService);
  });

  it('validates allowed identifiers', () => {
    const allowed = ['abc', 'A1_-', 'user123', 'ID_456-XYZ'];
    for (const candidate of allowed) {
      expect(service.isValidUserId(candidate)).withContext(candidate).toBeTrue();
    }

    const sixtyFourChars = 'a'.repeat(64);
    expect(service.isValidUserId(sixtyFourChars)).toBeTrue();
  });

  it('rejects invalid identifiers', () => {
    const disallowed = ['', ' ', 'abc!', 'user@domain', 'äöü', 'a'.repeat(65)];
    for (const candidate of disallowed) {
      expect(service.isValidUserId(candidate)).withContext(candidate).toBeFalse();
    }
  });
});
