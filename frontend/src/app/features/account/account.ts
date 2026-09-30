import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideEye, LucideEyeOff } from '@lucide/angular';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { getErrorMessage } from '../../core/http-error';
import { TwoFactorSetup } from '../../core/models';
import { Alert } from '../../shared/alert';
import {
  PASSWORD_MAX_LENGTH,
  PASSWORD_RULES,
  passwordsMatch,
  showError,
  strongPassword,
} from '../../shared/form-validators';
import QRCode from 'qrcode';

type TwoFactorStep = 'idle' | 'setup' | 'codes';

@Component({
  selector: 'app-account',
  imports: [ReactiveFormsModule, Alert, LucideEye, LucideEyeOff],
  templateUrl: './account.html',
})
export class Account {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly showError = showError;
  protected readonly passwordMaxLength = PASSWORD_MAX_LENGTH;

  protected readonly user = this.auth.user;

  // ---- Change password ----

  protected readonly passwordMessage = signal<string | null>(null);
  protected readonly passwordError = signal<string | null>(null);
  protected readonly savingPassword = signal(false);
  protected readonly passwordSubmitted = signal(false);
  protected readonly showPassword = signal(false);

  protected readonly passwordForm = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      password: ['', [Validators.required, strongPassword]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch },
  );

  private readonly newPasswordValue = toSignal(this.passwordForm.controls.password.valueChanges, {
    initialValue: '',
  });

  protected readonly passwordRules = computed(() =>
    PASSWORD_RULES.map((rule) => ({
      label: rule.label,
      met: rule.test(this.newPasswordValue()),
    })),
  );

  // ---- Two-factor ----

  protected readonly twoFactorStep = signal<TwoFactorStep>('idle');
  protected readonly setup = signal<TwoFactorSetup | null>(null);
  protected readonly qrCodeDataUrl = signal<string | null>(null);
  protected readonly recoveryCodes = signal<string[]>([]);
  protected readonly twoFactorBusy = signal(false);
  protected readonly twoFactorError = signal<string | null>(null);
  protected readonly twoFactorMessage = signal<string | null>(null);
  protected readonly twoFactorSubmitted = signal(false);

  protected readonly enableForm = this.fb.group({
    code: ['', Validators.required],
  });

  protected readonly confirmPasswordForm = this.fb.group({
    password: ['', Validators.required],
  });

  // ---- Change password ----

  protected togglePassword(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected showMismatch(): boolean {
    const confirm = this.passwordForm.controls.confirmPassword;

    return (
      this.passwordForm.hasError('passwordsMismatch') &&
      (confirm.touched || this.passwordSubmitted())
    );
  }

  protected changePassword(): void {
    this.passwordSubmitted.set(true);
    this.passwordError.set(null);
    this.passwordMessage.set(null);

    if (this.passwordForm.invalid || this.savingPassword()) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const value = this.passwordForm.getRawValue();

    this.savingPassword.set(true);

    this.auth
      .changePassword({
        currentPassword: value.currentPassword,
        newPassword: value.password,
        confirmPassword: value.confirmPassword,
      })
      .pipe(
        finalize(() => this.savingPassword.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.passwordMessage.set(response.message);
          this.passwordForm.reset();
          this.passwordSubmitted.set(false);
        },
        error: (error) => this.passwordError.set(getErrorMessage(error)),
      });
  }

  // ---- Two-factor ----

  protected startTwoFactorSetup(): void {
    this.twoFactorError.set(null);
    this.twoFactorMessage.set(null);
    this.twoFactorBusy.set(true);

    this.auth
      .getTwoFactorSetup()
      .pipe(
        finalize(() => this.twoFactorBusy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (setup) => {
          this.setup.set(setup);
          this.twoFactorStep.set('setup');
          this.renderQrCode(setup.authenticatorUri);
        },
        error: (error) => this.twoFactorError.set(getErrorMessage(error)),
      });
  }

  protected enableTwoFactor(): void {
    this.twoFactorSubmitted.set(true);
    this.twoFactorError.set(null);

    if (this.enableForm.invalid || this.twoFactorBusy()) {
      this.enableForm.markAllAsTouched();
      return;
    }

    this.twoFactorBusy.set(true);

    this.auth
      .enableTwoFactor(this.enableForm.getRawValue().code.trim())
      .pipe(
        finalize(() => this.twoFactorBusy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.recoveryCodes.set(response.recoveryCodes);
          this.twoFactorStep.set('codes');
          this.enableForm.reset();
          this.twoFactorSubmitted.set(false);
          this.qrCodeDataUrl.set(null);
          this.setup.set(null);
          this.refreshUser();
        },
        error: (error) => this.twoFactorError.set(getErrorMessage(error)),
      });
  }

  protected disableTwoFactor(): void {
    this.twoFactorSubmitted.set(true);
    this.twoFactorError.set(null);
    this.twoFactorMessage.set(null);

    if (this.confirmPasswordForm.invalid || this.twoFactorBusy()) {
      this.confirmPasswordForm.markAllAsTouched();
      return;
    }

    if (!confirm('Turn off two-factor authentication? Your account will be less protected.')) {
      return;
    }

    this.twoFactorBusy.set(true);

    this.auth
      .disableTwoFactor(this.confirmPasswordForm.getRawValue().password)
      .pipe(
        finalize(() => this.twoFactorBusy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.twoFactorMessage.set(response.message);
          this.resetTwoFactorForms();
          this.refreshUser();
        },
        error: (error) => this.twoFactorError.set(getErrorMessage(error)),
      });
  }

  protected regenerateRecoveryCodes(): void {
    this.twoFactorSubmitted.set(true);
    this.twoFactorError.set(null);
    this.twoFactorMessage.set(null);

    if (this.confirmPasswordForm.invalid || this.twoFactorBusy()) {
      this.confirmPasswordForm.markAllAsTouched();
      return;
    }

    this.twoFactorBusy.set(true);

    this.auth
      .regenerateRecoveryCodes(this.confirmPasswordForm.getRawValue().password)
      .pipe(
        finalize(() => this.twoFactorBusy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.recoveryCodes.set(response.recoveryCodes);
          this.twoFactorStep.set('codes');
          this.resetTwoFactorForms();
        },
        error: (error) => this.twoFactorError.set(getErrorMessage(error)),
      });
  }

  protected cancelTwoFactorSetup(): void {
    this.twoFactorStep.set('idle');
    this.setup.set(null);
    this.qrCodeDataUrl.set(null);
    this.resetTwoFactorForms();
  }

  protected dismissRecoveryCodes(): void {
    this.recoveryCodes.set([]);
    this.twoFactorStep.set('idle');
  }

  protected copyRecoveryCodes(): void {
    navigator.clipboard
      ?.writeText(this.recoveryCodes().join('\n'))
      .then(() => this.twoFactorMessage.set('Recovery codes copied to the clipboard.'))
      .catch(() => this.twoFactorError.set('Could not copy the codes. Please select and copy them.'));
  }

  private renderQrCode(uri: string): void {
    QRCode.toDataURL(uri, { width: 220, margin: 1 })
      .then((dataUrl) => this.qrCodeDataUrl.set(dataUrl))
      // The shared key is shown as well, so the page still works without the image
      .catch(() => this.qrCodeDataUrl.set(null));
  }

  private resetTwoFactorForms(): void {
    this.enableForm.reset();
    this.confirmPasswordForm.reset();
    this.twoFactorSubmitted.set(false);
  }

  private refreshUser(): void {
    this.auth.refreshUser().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => {} });
  }
}
