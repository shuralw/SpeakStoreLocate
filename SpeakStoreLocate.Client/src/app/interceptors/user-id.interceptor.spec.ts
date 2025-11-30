import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { UserIdInterceptor } from './user-id.interceptor';
import { UserIdService } from '../services/user-id.service';

class UserIdServiceStub {
  userId: string | null = null;

  getUserId(): string | null {
    return this.userId;
  }

  isValidUserId(id: string | null | undefined): boolean {
    if (!id) {
      return false;
    }
    return /^[A-Za-z0-9_-]{1,64}$/.test(id);
  }
}

describe('UserIdInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let userIdServiceStub: UserIdServiceStub;

  beforeEach(() => {
    userIdServiceStub = new UserIdServiceStub();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: HTTP_INTERCEPTORS, useClass: UserIdInterceptor, multi: true },
        { provide: UserIdService, useValue: userIdServiceStub },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches X-User-Id header when available and valid', () => {
    userIdServiceStub.userId = 'ValidUser_123';

    http.get('/api/test').subscribe();

    const request = httpMock.expectOne('/api/test');
    expect(request.request.headers.get('X-User-Id')).toBe('ValidUser_123');
    request.flush({});
  });

  it('leaves request untouched when user id missing', () => {
    userIdServiceStub.userId = null;

    http.get('/api/test').subscribe();

    const request = httpMock.expectOne('/api/test');
    expect(request.request.headers.has('X-User-Id')).toBeFalse();
    request.flush({});
  });
});
