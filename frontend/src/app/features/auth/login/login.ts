import { HttpErrorResponse } from '@angular/common/http';
import { Location } from '@angular/common';
import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { homeFor, safeReturnUrl } from '../../../core/auth/auth.guards';
import { AuthService } from '../../../core/auth/auth.service';
import { UserRole } from '../../../core/models';
import { getErrorMessage } from '../../../core/http-error';
import { Alert } from '../../../shared/alert';
import { showError } from '../../../shared/form-validators';
import { AuthCard } from '../auth-card';
import { GoogleButton } from '../google-button';

interface LoginPageState {
  registered?: boolean;
  email?: string;
  message?: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, Alert, AuthCard, GoogleButton],
  templateUrl: './login.html',
})
export class Login {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // Bound from ?returnUrl=
  readonly returnUrl = input<string>();

  // Bound from the route data: 'Customer' on /login, 'Admin' on /admin/login
  readonly portal = input<UserRole>('Customer');

  protected readonly isAdminPortal = computed(() => this.portal() === 'Admin');

  // Set when the credentials belong to the other portal
  protected readonly wrongPortal = signal(false);

  protected readonly showError = showError;

  // Set when arriving from the register or reset-password page
  private readonly pageState = (inject(Location).getState() ?? {}) as LoginPageState;

  protected readonly successMessage = signal<string | null>(
    this.pageState.message ??
      (this.pageState.registered ? 'Account created. You can sign in now.' : null),
  );
  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);
  protected readonly showPassword = signal(false);

  // The password was accepted and a 2FA code is now needed
  protected readonly twoFactorStep = signal(false);
  protected readonly useRecoveryCode = signal(false);
  protected readonly resending = signal(false);

  // Shown when sign-in failed because the email is not confirmed yet
  protected readonly needsConfirmation = signal(false);

  protected readonly form = this.fb.group({
    email: [this.pageState.email ?? '', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    rememberMe: [false],
  });

  protected readonly twoFactorForm = this.fb.group({
    code: ['', Validators.required],
    rememberMachine: [false],
  });

  protected togglePassword(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected toggleCodeType(): void {
    this.useRecoveryCode.update((useRecovery) => !useRecovery);
    this.twoFactorForm.controls.code.reset();
    this.error.set(null);
  }

  protected submit(): void {
    this.submitted.set(true);
    this.error.set(null);
    this.needsConfirmation.set(false);
    this.wrongPortal.set(false);

    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.successMessage.set(null);

    this.auth
      .login({ ...this.form.getRawValue(), portal: this.portal() })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          if (response.requiresTwoFactor) {
            this.twoFactorStep.set(true);
            this.submitted.set(false);
            return;
          }

          this.goHome();
        },
        error: (error) => {
          const message = getErrorMessage(error);

          // 403 means the password was right but this is the wrong sign-in page
          this.wrongPortal.set(
            error instanceof HttpErrorResponse && error.status === 403,
          );
          this.needsConfirmation.set(message.toLowerCase().includes('confirm your email'));
          this.error.set(message);
          this.form.controls.password.reset();
        },
      });
  }

  protected submitTwoFactor(): void {
    this.submitted.set(true);
    this.error.set(null);

    if (this.twoFactorForm.invalid || this.submitting()) {
      this.twoFactorForm.markAllAsTouched();
      return;
    }

    const value = this.twoFactorForm.getRawValue();

    this.submitting.set(true);

    this.auth
      .verifyTwoFactor({
        code: value.code.trim(),
        isRecoveryCode: this.useRecoveryCode(),
        rememberMe: this.form.getRawValue().rememberMe,
        rememberMachine: value.rememberMachine,
      })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.goHome(),
        error: (error) => {
          this.error.set(getErrorMessage(error));
          this.twoFactorForm.controls.code.reset();
        },
      });
  }

  // Admins land in the admin dashboard, customers in the shop
  private goHome(): void {
    const fallback = homeFor(this.auth.user()?.role);

    this.router.navigateByUrl(safeReturnUrl(this.returnUrl(), fallback));
  }

  protected backToPassword(): void {
    this.twoFactorStep.set(false);
    this.useRecoveryCode.set(false);
    this.twoFactorForm.reset();
    this.error.set(null);
    this.submitted.set(false);
  }

  protected resendConfirmation(): void {
    const email = this.form.getRawValue().email.trim();

    if (!email || this.resending()) {
      return;
    }

    this.resending.set(true);
    this.error.set(null);

    this.auth
      .resendConfirmation(email)
      .pipe(
        finalize(() => this.resending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.needsConfirmation.set(false);
          this.successMessage.set(response.message);
        },
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }
}
