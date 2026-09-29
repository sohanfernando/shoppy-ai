import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { getErrorMessage } from '../../../core/http-error';
import { Alert } from '../../../shared/alert';
import { showError } from '../../../shared/form-validators';
import { AuthCard } from '../auth-card';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, Alert, AuthCard],
  templateUrl: './forgot-password.html',
})
export class ForgotPassword {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly showError = showError;

  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);

  protected readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  protected submit(): void {
    this.submitted.set(true);
    this.error.set(null);

    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    this.auth
      .forgotPassword(this.form.getRawValue().email.trim())
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => this.message.set(response.message),
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }
}
