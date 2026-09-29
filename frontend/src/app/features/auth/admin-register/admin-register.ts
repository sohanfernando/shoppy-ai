import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { getErrorMessage } from '../../../core/http-error';
import { Alert } from '../../../shared/alert';
import {
  PASSWORD_MAX_LENGTH,
  PASSWORD_RULES,
  notBlank,
  passwordsMatch,
  showError,
  strongPassword,
} from '../../../shared/form-validators';
import { AuthCard } from '../auth-card';

// Must match the column lengths in the backend
const NAME_MAX_LENGTH = 150;
const EMAIL_MAX_LENGTH = 200;

@Component({
  selector: 'app-admin-register',
  imports: [ReactiveFormsModule, RouterLink, Alert, AuthCard],
  templateUrl: './admin-register.html',
})
export class AdminRegister {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly showError = showError;
  protected readonly nameMaxLength = NAME_MAX_LENGTH;
  protected readonly emailMaxLength = EMAIL_MAX_LENGTH;
  protected readonly passwordMaxLength = PASSWORD_MAX_LENGTH;

  protected readonly error = signal<string | null>(null);

  // Set when the account needs its email confirmed before signing in
  protected readonly confirmationSent = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);
  protected readonly showPassword = signal(false);

  protected readonly form = this.fb.group(
    {
      fullName: ['', [Validators.required, notBlank, Validators.maxLength(NAME_MAX_LENGTH)]],
      email: [
        '',
        [Validators.required, Validators.email, Validators.maxLength(EMAIL_MAX_LENGTH)],
      ],
      password: ['', [Validators.required, strongPassword]],
      confirmPassword: ['', Validators.required],
      registrationKey: ['', [Validators.required, notBlank]],
    },
    { validators: passwordsMatch },
  );

  private readonly passwordValue = toSignal(this.form.controls.password.valueChanges, {
    initialValue: '',
  });

  protected readonly passwordRules = computed(() =>
    PASSWORD_RULES.map((rule) => ({ label: rule.label, met: rule.test(this.passwordValue()) })),
  );

  protected showMismatch(): boolean {
    const confirm = this.form.controls.confirmPassword;

    return (
      this.form.hasError('passwordsMismatch') && (confirm.touched || this.submitted())
    );
  }

  protected togglePassword(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected submit(): void {
    this.submitted.set(true);
    this.error.set(null);

    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    this.submitting.set(true);

    this.auth
      .register({
        ...value,
        fullName: value.fullName.trim(),
        email: value.email.trim(),
        registrationKey: value.registrationKey.trim(),
      })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          const email = value.email.trim();

          // Unconfirmed accounts cannot sign in yet, so stay here and explain
          if (!response.canSignIn) {
            this.confirmationSent.set(response.message);
            return;
          }

          this.router.navigate(['/login'], {
            state: { registered: true, email, message: response.message },
          });
        },
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }
}
