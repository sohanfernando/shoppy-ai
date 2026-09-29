import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
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
  passwordsMatch,
  showError,
  strongPassword,
} from '../../../shared/form-validators';
import { AuthCard } from '../auth-card';

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, Alert, AuthCard],
  templateUrl: './reset-password.html',
})
export class ResetPassword {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // Both come from the link in the reset email
  readonly userId = input<string>();
  readonly token = input<string>();

  protected readonly showError = showError;
  protected readonly passwordMaxLength = PASSWORD_MAX_LENGTH;

  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);
  protected readonly showPassword = signal(false);

  protected readonly hasValidLink = computed(() => {
    const userId = Number(this.userId());

    return Number.isInteger(userId) && userId > 0 && !!this.token();
  });

  protected readonly form = this.fb.group(
    {
      password: ['', [Validators.required, strongPassword]],
      confirmPassword: ['', Validators.required],
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

    return this.form.hasError('passwordsMismatch') && (confirm.touched || this.submitted());
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
      .resetPassword({
        userId: Number(this.userId()),
        token: this.token() ?? '',
        password: value.password,
        confirmPassword: value.confirmPassword,
      })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) =>
          this.router.navigate(['/login'], { state: { message: response.message } }),
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }
}
