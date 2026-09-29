import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { UserRole } from '../models';
import { AuthService } from './auth.service';

export const ADMIN_HOME = '/admin/orders';
export const CUSTOMER_HOME = '/dashboard';

export function homeFor(role: UserRole | undefined): string {
  return role === 'Admin' ? ADMIN_HOME : CUSTOMER_HOME;
}

// Only allow in-app paths, so ?returnUrl= can't send users to another site
export function safeReturnUrl(url: string | null | undefined, fallback: string): string {
  const isInternal =
    !!url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\');

  const isAuthPage =
    !!url &&
    (url.startsWith('/login') ||
      url.startsWith('/register') ||
      url.startsWith('/admin/login') ||
      url.startsWith('/admin/register'));

  return isInternal && !isAuthPage ? url : fallback;
}

function roleGuard(role: UserRole): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    return auth.ensureSession().pipe(
      map((signedIn) => {
        if (!signedIn) {
          const loginPage = role === 'Admin' ? '/admin/login' : '/login';

          return router.createUrlTree([loginPage], {
            queryParams: { returnUrl: state.url },
          });
        }

        // Signed in with the other role: send them to their own area
        return auth.user()?.role === role
          ? true
          : router.createUrlTree([homeFor(auth.user()?.role)]);
      }),
    );
  };
}

export const adminGuard = roleGuard('Admin');
export const customerGuard = roleGuard('Customer');

// Signed-in users don't need the login / register pages
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth
    .ensureSession()
    .pipe(
      map((signedIn) =>
        signedIn ? router.createUrlTree([homeFor(auth.user()?.role)]) : true,
      ),
    );
};
