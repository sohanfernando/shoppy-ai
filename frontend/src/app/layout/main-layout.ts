import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { APP_NAME } from '../core/config';

interface NavLink {
  label: string;
  path: string;
}

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.html',
})
export class MainLayout {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly appName = APP_NAME;
  protected readonly user = this.auth.user;
  protected readonly signingOut = signal(false);
  protected readonly signOutError = signal(false);

  protected readonly navLinks: NavLink[] = [
    { label: 'Orders', path: '/orders' },
    { label: 'Products', path: '/products' },
    { label: 'Customers', path: '/customers' },
  ];

  protected signOut(): void {
    if (this.signingOut()) {
      return;
    }

    this.signingOut.set(true);
    this.signOutError.set(false);

    this.auth.logout().subscribe({
      next: () => {
        this.signingOut.set(false);
        this.router.navigate(['/login']);
      },
      error: () => {
        this.signingOut.set(false);
        this.signOutError.set(true);
      },
    });
  }
}
