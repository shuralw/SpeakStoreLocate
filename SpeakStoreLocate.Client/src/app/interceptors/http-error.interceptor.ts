import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { UserIdService } from '../services/user-id.service';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  constructor(
    private readonly snackBar: MatSnackBar,
    private readonly userIdService: UserIdService,
  ) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 400 && this.isUserIdHeaderError(error)) {
          this.snackBar.open('Bitte User-ID setzen oder prüfen.', 'Schliessen', {
            duration: 5000,
            panelClass: ['user-id-warning-snackbar'],
          });
          this.userIdService.requestFocus();
        }

        return throwError(() => error);
      }),
    );
  }

  private isUserIdHeaderError(error: HttpErrorResponse): boolean {
    const payload = this.extractMessage(error).toLowerCase();
    return payload.includes('missing x-user-id header') || payload.includes('invalid x-user-id header');
  }

  private extractMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string') {
      return error.error;
    }

    if (error.error && typeof (error.error as any).message === 'string') {
      return (error.error as any).message;
    }

    return error.message ?? '';
  }
}
