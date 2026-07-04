import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService, SKIP_AUTH_REFRESH } from './auth.service';

/**
 * Attaches the in-memory access token to /api requests and, on a 401,
 * tries one silent refresh before failing.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const authorized = withToken(req, auth.token());
  if (req.context.get(SKIP_AUTH_REFRESH)) {
    return next(authorized);
  }

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return auth.refresh().pipe(
          switchMap((refreshed) =>
            refreshed ? next(withToken(req, auth.token())) : throwError(() => error),
          ),
        );
      }

      return throwError(() => error);
    }),
  );
};

function withToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token || !req.url.startsWith('/api')) {
    return req;
  }

  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
