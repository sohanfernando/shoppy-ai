import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { APP_NAME } from '../core/config';
import { AppShell, NavItem } from './app-shell';
import { NotificationBell } from './notification-bell';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NotificationBell],
  templateUrl: './admin-layout.html',
})
export class AdminLayout extends AppShell {
  protected readonly appName = APP_NAME;

  protected readonly navItems: NavItem[] = [
    { label: 'Orders', path: '/admin/orders', icon: 'orders' },
    { label: 'Products', path: '/admin/products', icon: 'products' },
    { label: 'Customers', path: '/admin/customers', icon: 'customers' },
    { label: 'Reviews', path: '/admin/reviews', icon: 'reviews' },
    { label: 'Account', path: '/admin/account', icon: 'account' },
  ];

  constructor() {
    super();
  }
}
