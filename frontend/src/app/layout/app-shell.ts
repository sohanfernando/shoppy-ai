import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { APP_NAME } from '../core/config';
import { NotificationService } from '../core/services/notification.service';

export interface NavItem {
  label: string;
  path: string;
  icon: string;
  exact?: boolean;
}

// Shared behaviour of both sidebar layouts: sign out, live notifications, mobile menu
@Component({ template: '' })
export abstract class AppShell {
  protected readonly auth = inject(AuthService);
  protected readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly titleService = inject(Title);

  protected readonly user = this.auth.user;
  protected readonly toast = this.notifications.toast;
  protected readonly signingOut = signal(false);
  protected readonly sidebarOpen = signal(false);

  // The current route's title (e.g. "Products"), read off the <title> tag
  // PageTitleStrategy already sets — so the header always matches the page.
  protected readonly pageTitle = signal(this.currentPageTitle());

  protected constructor() {
    this.notifications.load();
    void this.notifications.connect();

    // Close the mobile sidebar and refresh the header title whenever the page changes
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.sidebarOpen.set(false);
        this.pageTitle.set(this.currentPageTitle());
      });
  }

  private currentPageTitle(): string {
    const suffix = ` · ${APP_NAME}`;
    const title = this.titleService.getTitle();

    return title.endsWith(suffix) ? title.slice(0, -suffix.length) : APP_NAME;
  }

  protected toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  protected closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  protected dismissToast(): void {
    this.notifications.clearToast();
  }

  protected openToast(link: string): void {
    this.notifications.clearToast();

    if (link) {
      this.router.navigateByUrl(link);
    }
  }

  protected signOut(): void {
    if (this.signingOut()) {
      return;
    }

    this.signingOut.set(true);

    this.auth
      .logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        complete: () => {
          void this.notifications.disconnect();
          this.signingOut.set(false);
          this.router.navigate(['/']);
        },
      });
  }
}
