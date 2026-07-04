import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorKey } from './i18n';
import { ApiErrorBody } from './auth/auth.models';

/** Translation key for the first error code in an API error response. */
export function errorKeyFromResponse(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as ApiErrorBody | null;
    const code = body?.errors?.[0]?.code;
    if (code) {
      return apiErrorKey(code);
    }
  }

  return 'apiErrors.generic';
}
