import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { API_BASE_URL } from '../config';
import {
  AuthUser,
  ChangePasswordRequest,
  CustomerRegisterRequest,
  ConfirmEmailRequest,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  RecoveryCodes,
  RegisterRequest,
  RegisterResponse,
  ResetPasswordRequest,
  TwoFactorLoginRequest,
  TwoFactorSetup,
} from '../models';

export const AUTH_API_URL = `${API_BASE_URL}/auth`;

type AuthStatus = 'unknown' | 'authenticated' | 'anonymous';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly currentUser = signal<AuthUser | null>(null);
  private readonly status = signal<AuthStatus>('unknown');

  // Shared so parallel guard checks only call /me once
  private sessionCheck$: Observable<boolean> | null = null;

  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.status() === 'authenticated');
  readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');
  readonly isCustomer = computed(() => this.currentUser()?.role === 'Customer');

  // ---- Session ----

  // Restores the session from the auth cookie the first time it is needed
  ensureSession(): Observable<boolean> {
    if (this.status() !== 'unknown') {
      return of(this.isAuthenticated());
    }

    this.sessionCheck$ ??= this.http.get<AuthUser>(`${AUTH_API_URL}/me`).pipe(
      map((user) => {
        this.setUser(user);
        return true;
      }),
      catchError(() => {
        this.clearSession();
        return of(false);
      }),
      finalize(() => (this.sessionCheck$ = null)),
      shareReplay(1),
    );

    return this.sessionCheck$;
  }

  refreshUser(): Observable<AuthUser> {
    return this.http
      .get<AuthUser>(`${AUTH_API_URL}/me`)
      .pipe(tap((user) => this.setUser(user)));
  }

  // ---- Sign in and sign up ----

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${AUTH_API_URL}/login`, request).pipe(
      tap((response) => {
        if (response.user) {
          this.setUser(response.user);
        }
      }),
    );
  }

  verifyTwoFactor(request: TwoFactorLoginRequest): Observable<AuthUser> {
    return this.http
      .post<AuthUser>(`${AUTH_API_URL}/2fa/verify`, request)
      .pipe(tap((user) => this.setUser(user)));
  }

  // Admin sign-up, which needs the registration key
  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${AUTH_API_URL}/register`, request);
  }

  registerCustomer(request: CustomerRegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${AUTH_API_URL}/register-customer`, request);
  }

  // Full page redirect: Google will send the browser back to the API
  startGoogleSignIn(returnUrl?: string): void {
    const query = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '';

    window.location.href = `${AUTH_API_URL}/google/start${query}`;
  }

  // Always clears the local session, even if the request fails
  logout(): Observable<void> {
    return this.http.post<void>(`${AUTH_API_URL}/logout`, null).pipe(
      catchError(() => of(undefined)),
      finalize(() => this.clearSession()),
    );
  }

  // ---- Email confirmation ----

  confirmEmail(request: ConfirmEmailRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${AUTH_API_URL}/confirm-email`, request);
  }

  resendConfirmation(email: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${AUTH_API_URL}/resend-confirmation`, { email });
  }

  // ---- Passwords ----

  forgotPassword(email: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${AUTH_API_URL}/forgot-password`, { email });
  }

  resetPassword(request: ResetPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${AUTH_API_URL}/reset-password`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${AUTH_API_URL}/change-password`, request);
  }

  // ---- Two-factor authentication ----

  getTwoFactorSetup(): Observable<TwoFactorSetup> {
    return this.http.get<TwoFactorSetup>(`${AUTH_API_URL}/2fa/setup`);
  }

  enableTwoFactor(code: string): Observable<RecoveryCodes> {
    return this.http.post<RecoveryCodes>(`${AUTH_API_URL}/2fa/enable`, { code });
  }

  disableTwoFactor(password: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${AUTH_API_URL}/2fa/disable`, { password });
  }

  regenerateRecoveryCodes(password: string): Observable<RecoveryCodes> {
    return this.http.post<RecoveryCodes>(`${AUTH_API_URL}/2fa/recovery-codes`, { password });
  }

  // ---- Internal ----

  clearSession(): void {
    this.currentUser.set(null);
    this.status.set('anonymous');
  }

  private setUser(user: AuthUser): void {
    this.currentUser.set(user);
    this.status.set('authenticated');
  }
}
