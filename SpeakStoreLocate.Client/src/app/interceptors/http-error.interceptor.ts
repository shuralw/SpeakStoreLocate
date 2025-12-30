import { Injectable } from '@angular/core';
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { UserIdService } from '../services/user-id.service';
import { isUserIdHeaderError } from '../utils/http-error.utils';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  constructor(
    private readonly snackBar: MatSnackBar,
    private readonly userIdService: UserIdService,
  ) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (isUserIdHeaderError(error)) {
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
}
