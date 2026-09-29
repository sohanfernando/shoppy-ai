import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { API_BASE_URL } from '../config';
import { AUTH_API_URL, AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(API_BASE_URL)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);

  // Send the auth cookie to the API
  return next(request.clone({ withCredentials: true })).pipe(
    catchError((error: unknown) => {
      // Auth endpoints handle their own 401s (wrong password, no session yet)
      const sessionExpired =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !request.url.startsWith(AUTH_API_URL);

      if (sessionExpired) {
        const returnUrl = router.url;
        const loginPage = returnUrl.startsWith('/admin') ? '/admin/login' : '/login';

        auth.clearSession();
        router.navigate([loginPage], { queryParams: { returnUrl } });
      }

      return throwError(() => error);
    }),
  );
};
