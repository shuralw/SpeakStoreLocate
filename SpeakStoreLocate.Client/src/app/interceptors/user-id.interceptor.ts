import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserIdService } from '../services/user-id.service';

@Injectable()
export class UserIdInterceptor implements HttpInterceptor {
  constructor(private readonly userIdService: UserIdService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const userId = this.userIdService.getUserId();
    if (!userId || !this.userIdService.isValidUserId(userId)) {
      return next.handle(req);
    }

    const requestWithHeader = req.clone({
      setHeaders: {
        'X-User-Id': userId,
      },
    });

    return next.handle(requestWithHeader);
  }
}
