import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { getErrorMessage } from '../../../core/http-error';
import { Alert } from '../../../shared/alert';
import { AuthCard } from '../auth-card';

@Component({
  selector: 'app-confirm-email',
  imports: [RouterLink, Alert, AuthCard],
  templateUrl: './confirm-email.html',
})
export class ConfirmEmail {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  // Both come from the link in the confirmation email
  readonly userId = input.required<string>();
  readonly token = input.required<string>();

  protected readonly confirming = signal(true);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const userId = Number(this.userId());
      const token = this.token();

      if (!Number.isInteger(userId) || userId <= 0 || !token) {
        this.error.set('This confirmation link is not valid.');
        this.confirming.set(false);
        return;
      }

      this.auth
        .confirmEmail({ userId, token })
        .pipe(
          finalize(() => this.confirming.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: (response) => this.message.set(response.message),
          error: (error) => this.error.set(getErrorMessage(error)),
        });
    });
  }
}
