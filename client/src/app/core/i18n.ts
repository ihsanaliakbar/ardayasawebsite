import { TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import id from '../../i18n/id.json';

/**
 * Loads translations from statically imported JSON so the same code path works
 * in the browser and during SSR (no HTTP round-trip for resource files).
 * Adding English later = add en.json and switch on `lang`.
 */
export class StaticTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<TranslationObject> {
    return of(id as TranslationObject);
  }
}

/** Maps a backend error code like "auth.invalid_credentials" to a translation key. */
export function apiErrorKey(code: string): string {
  return `apiErrors.${code.replace(/\./g, '_')}`;
}
