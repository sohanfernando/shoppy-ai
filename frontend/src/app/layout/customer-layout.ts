import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  LucideBell,
  LucideClipboardList,
  LucideLayoutDashboard,
  LucideLogOut,
  LucideMenu,
  LucideShoppingBag,
  LucideShoppingCart,
  LucideUser,
} from '@lucide/angular';
import { APP_NAME } from '../core/config';
import { CartService } from '../core/services/cart.service';
import { AppShell, NavItem } from './app-shell';
import { NotificationBell } from './notification-bell';

@Component({
  selector: 'app-customer-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NotificationBell,
    LucideBell,
    LucideClipboardList,
    LucideLayoutDashboard,
    LucideLogOut,
    LucideMenu,
    LucideShoppingBag,
    LucideShoppingCart,
    LucideUser,
  ],
  templateUrl: './customer-layout.html',
})
export class CustomerLayout extends AppShell {
  private readonly cart = inject(CartService);

  protected readonly appName = APP_NAME;
  protected readonly cartCount = this.cart.itemCount;

  protected readonly navItems: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'dashboard' },
    { label: 'Products', path: '/products', icon: 'products' },
    { label: 'My orders', path: '/orders', icon: 'orders' },
    { label: 'Notifications', path: '/notifications', icon: 'notifications' },
    { label: 'Account', path: '/account', icon: 'account' },
  ];

  constructor() {
    super();
  }

  protected initials(fullName: string): string {
    return fullName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join('');
  }
}
