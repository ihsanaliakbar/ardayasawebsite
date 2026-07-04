import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Guard factory: requires login and, when given, at least one of the roles. */
export function roleGuard(...roles: string[]): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/masuk'], { queryParams: { redirect: state.url } });
    }

    if (roles.length > 0 && !roles.some((r) => auth.hasRole(r))) {
      return router.createUrlTree(['/']);
    }

    return true;
  };
}
