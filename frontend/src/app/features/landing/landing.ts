import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  LucideBarChart3,
  LucideBell,
  LucideClock,
  LucideShieldCheck,
  LucideShoppingCart,
  LucideStar,
} from '@lucide/angular';
import { homeFor } from '../../core/auth/auth.guards';
import { AuthService } from '../../core/auth/auth.service';
import { APP_NAME } from '../../core/config';
import { GoogleButton } from '../auth/google-button';

interface Feature {
  title: string;
  description: string;
  icon: string;
}

interface Step {
  title: string;
  description: string;
}

@Component({
  selector: 'app-landing',
  imports: [
    RouterLink,
    GoogleButton,
    LucideBarChart3,
    LucideBell,
    LucideClock,
    LucideShieldCheck,
    LucideShoppingCart,
    LucideStar,
  ],
  templateUrl: './landing.html',
})
export class Landing {
  private readonly auth = inject(AuthService);

  protected readonly appName = APP_NAME;
  protected readonly year = new Date().getFullYear();

  protected readonly user = this.auth.user;
  protected readonly checkingSession = signal(true);

  // Where the top-right button goes once we know who is signed in
  protected readonly homeLink = computed(() => homeFor(this.user()?.role));

  protected readonly features: Feature[] = [
    {
      title: 'Order in a few clicks',
      description:
        'Browse the catalogue, fill your cart and check out. Stock is reserved the moment you order.',
      icon: 'cart',
    },
    {
      title: 'Track every order',
      description:
        'Your full order history with items, totals and status. Changed your mind? Cancel it yourself.',
      icon: 'clock',
    },
    {
      title: 'Know what you spend',
      description:
        'A dashboard with your monthly spending, what you bought most and how much discounts saved you.',
      icon: 'chart',
    },
    {
      title: 'Live notifications',
      description:
        'Updates arrive the second something changes, without refreshing the page.',
      icon: 'bell',
    },
    {
      title: 'Reviews that count',
      description:
        'Rate and review the products you actually ordered, so other customers can trust them.',
      icon: 'star',
    },
    {
      title: 'Your account, protected',
      description:
        'Confirmed email addresses, password resets and optional two-factor authentication.',
      icon: 'shield',
    },
  ];

  protected readonly steps: Step[] = [
    {
      title: 'Create your account',
      description: 'Sign up with your email or continue with Google. Confirm your address and you are in.',
    },
    {
      title: 'Add products to your cart',
      description: 'Search the catalogue, check what is in stock and choose your quantities.',
    },
    {
      title: 'Place the order and follow it',
      description: 'See totals, watch the status and review what you bought afterwards.',
    },
  ];

  constructor() {
    // Lets the page greet a signed-in visitor instead of asking them to sign in again
    this.auth
      .ensureSession()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: () => this.checkingSession.set(false),
        error: () => this.checkingSession.set(false),
      });
  }
}
