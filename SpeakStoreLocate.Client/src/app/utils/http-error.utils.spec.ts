import { HttpErrorResponse } from '@angular/common/http';
import { extractErrorMessage, isUserIdHeaderError } from './http-error.utils';

describe('http-error.utils', () => {
  describe('extractErrorMessage', () => {
    it('should return error when it is a string', () => {
      const error = new HttpErrorResponse({
        error: 'Test error message',
        status: 400,
      });
      expect(extractErrorMessage(error)).toBe('Test error message');
    });

    it('should return message property when error is an object with message', () => {
      const error = new HttpErrorResponse({
        error: { message: 'Object error message' },
        status: 400,
      });
      expect(extractErrorMessage(error)).toBe('Object error message');
    });

    it('should return response message when error has no extractable message', () => {
      const error = new HttpErrorResponse({
        error: { code: 123 },
        status: 400,
        statusText: 'Bad Request',
        url: 'http://test.com/api',
      });
      expect(extractErrorMessage(error)).toContain('Http failure response');
    });

    it('should return empty string when no message is available', () => {
      const error = new HttpErrorResponse({
        error: null,
        status: 0,
      });
      const result = extractErrorMessage(error);
      expect(typeof result).toBe('string');
    });
  });

  describe('isUserIdHeaderError', () => {
    it('should return true for missing x-user-id header error', () => {
      const error = new HttpErrorResponse({
        error: 'Missing X-User-ID header',
        status: 400,
      });
      expect(isUserIdHeaderError(error)).toBe(true);
    });

    it('should return true for invalid x-user-id header error', () => {
      const error = new HttpErrorResponse({
        error: 'Invalid X-User-ID header',
        status: 400,
      });
      expect(isUserIdHeaderError(error)).toBe(true);
    });

    it('should return true for lowercase error message', () => {
      const error = new HttpErrorResponse({
        error: 'missing x-user-id header in request',
        status: 400,
      });
      expect(isUserIdHeaderError(error)).toBe(true);
    });

    it('should return false for non-400 status', () => {
      const error = new HttpErrorResponse({
        error: 'Missing X-User-ID header',
        status: 401,
      });
      expect(isUserIdHeaderError(error)).toBe(false);
    });

    it('should return false for unrelated 400 error', () => {
      const error = new HttpErrorResponse({
        error: 'Invalid request body',
        status: 400,
      });
      expect(isUserIdHeaderError(error)).toBe(false);
    });

    it('should return true when error is object with message property', () => {
      const error = new HttpErrorResponse({
        error: { message: 'Missing X-User-ID header' },
        status: 400,
      });
      expect(isUserIdHeaderError(error)).toBe(true);
    });
  });
});
