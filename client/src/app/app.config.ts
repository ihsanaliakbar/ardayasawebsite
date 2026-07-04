import {
  ApplicationConfig,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, registerLocaleData } from '@angular/common';
import localeId from '@angular/common/locales/id';

// Dates and numbers render in Indonesian across the app (WIB display per SPEC).
registerLocaleData(localeId);
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateLoader, provideTranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { ssrBaseUrlInterceptor } from './core/ssr-base-url.interceptor';
import { AuthService } from './core/auth/auth.service';
import { StaticTranslateLoader } from './core/i18n';

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: 'id' },
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch(), withInterceptors([ssrBaseUrlInterceptor, authInterceptor])),
    provideTranslateService({
      loader: { provide: TranslateLoader, useClass: StaticTranslateLoader },
    }),
    // Restore the session from the httpOnly refresh cookie before first render (browser only).
    provideAppInitializer(() => {
      const platformId = inject(PLATFORM_ID);
      if (!isPlatformBrowser(platformId)) {
        return;
      }

      const auth = inject(AuthService);
      return firstValueFrom(auth.refresh()).then(() => undefined);
    }),
  ],
};
