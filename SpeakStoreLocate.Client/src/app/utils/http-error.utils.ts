import { HttpErrorResponse } from '@angular/common/http';

/**
 * Type guard to check if an object has a string message property.
 */
function hasStringMessage(obj: unknown): obj is { message: string } {
  return typeof obj === 'object' && obj !== null && 'message' in obj && typeof (obj as Record<string, unknown>)['message'] === 'string';
}

/**
 * Extracts the error message from an HttpErrorResponse.
 * Handles different response formats including string errors,
 * objects with a message property, or falls back to the response message.
 */
export function extractErrorMessage(error: HttpErrorResponse): string {
  if (typeof error.error === 'string') {
    return error.error;
  }

  if (hasStringMessage(error.error)) {
    return error.error.message;
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
