import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extracts the error message from an HttpErrorResponse.
 * Handles different response formats including string errors,
 * objects with a message property, or falls back to the response message.
 */
export function extractErrorMessage(error: HttpErrorResponse): string {
  if (typeof error.error === 'string') {
    return error.error;
  }

  if (error.error && typeof (error.error as { message?: unknown }).message === 'string') {
    return (error.error as { message: string }).message;
  }

  return error.message ?? '';
}

/**
 * Checks if an HttpErrorResponse indicates a missing or invalid X-User-ID header.
 * Returns true for 400 status errors that mention the X-User-ID header issue.
 */
export function isUserIdHeaderError(error: HttpErrorResponse): boolean {
  if (error.status !== 400) {
    return false;
  }

  const payload = extractErrorMessage(error).toLowerCase();
  return payload.includes('missing x-user-id header') || payload.includes('invalid x-user-id header');
}
