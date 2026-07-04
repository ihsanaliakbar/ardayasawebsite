import { HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';

// Read Node's process without requiring @types/node in the browser bundle.
const nodeEnv = (globalThis as { process?: { env?: Record<string, string | undefined> } }).process?.env;

/**
 * During SSR, HttpClient cannot resolve relative URLs, so public-content fetches
 * (e.g. GET /api/psychologists) would fail and pages would render without data.
 * Server-side we rewrite them to the API directly: API_BASE_URL in Docker
 * (http://api:8080), or the dev server origin under `ng serve` (whose
 * proxy.conf.json forwards /api to the backend). No-op in the browser.
 */
export function ssrBaseUrlInterceptor(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) {
  const platformId = inject(PLATFORM_ID);
  if (isPlatformServer(platformId) && req.url.startsWith('/')) {
    const base =
      nodeEnv?.['API_BASE_URL'] ?? `http://localhost:${nodeEnv?.['PORT'] ?? 4200}`;
    return next(req.clone({ url: new URL(req.url, base).toString() }));
  }

  return next(req);
}
