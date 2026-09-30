import { Routes } from '@angular/router';
import { adminGuard, customerGuard, guestGuard } from './core/auth/auth.guards';
import { AdminLayout } from './layout/admin-layout';
import { CustomerLayout } from './layout/customer-layout';

export const routes: Routes = [
  // ==================================
  // Public pages
  // ==================================
  {
    // Landing page. Customer pages keep the root URLs (/dashboard, /products, ...)
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/landing/landing').then((m) => m.Landing),
  },
  {
    path: 'login',
    title: 'Sign in',
    canActivate: [guestGuard],
    data: { portal: 'Customer' },
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    // Staff sign in on their own page; customer credentials are refused here
    path: 'admin/login',
    title: 'Staff sign in',
    canActivate: [guestGuard],
    data: { portal: 'Admin' },
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Create account',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    // Admin sign-up needs the registration key
    path: 'admin/register',
    title: 'Create admin account',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/admin-register/admin-register').then((m) => m.AdminRegister),
  },
  {
    path: 'confirm-email',
    title: 'Confirm email',
    loadComponent: () =>
      import('./features/auth/confirm-email/confirm-email').then((m) => m.ConfirmEmail),
  },
  {
    path: 'forgot-password',
    title: 'Forgot password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
  {
    path: 'reset-password',
    title: 'Reset password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password').then((m) => m.ResetPassword),
  },

  // ==================================
  // Admin dashboard
  // ==================================
  {
    path: 'admin',
    component: AdminLayout,
    canActivate: [adminGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'orders' },
      {
        path: 'dashboard',
        title: 'Dashboard',
        loadComponent: () =>
          import('./features/admin/dashboard/admin-dashboard').then((m) => m.AdminDashboard),
      },
      {
        path: 'orders',
        title: 'Orders',
        loadComponent: () =>
          import('./features/admin/orders/order-list/order-list').then((m) => m.OrderList),
      },
      {
        path: 'orders/:id',
        title: 'Order details',
        loadComponent: () =>
          import('./features/orders/order-detail/order-detail').then((m) => m.OrderDetail),
      },
      {
        path: 'products',
        title: 'Products',
        loadComponent: () =>
          import('./features/admin/products/product-list/product-list').then(
            (m) => m.ProductList,
          ),
      },
      {
        path: 'customers',
        title: 'Customers',
        loadComponent: () =>
          import('./features/admin/customers/customer-list/customer-list').then(
            (m) => m.CustomerList,
          ),
      },
      {
        path: 'reviews',
        title: 'Reviews',
        loadComponent: () =>
          import('./features/admin/reviews/review-list/review-list').then((m) => m.ReviewList),
      },
      {
        path: 'account',
        title: 'Account settings',
        loadComponent: () => import('./features/account/account').then((m) => m.Account),
      },
      {
        path: '**',
        title: 'Page not found',
        loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
      },
    ],
  },

  // ==================================
  // Customer area
  // ==================================
  {
    path: '',
    component: CustomerLayout,
    canActivate: [customerGuard],
    children: [
      {
        path: 'dashboard',
        title: 'Dashboard',
        loadComponent: () =>
          import('./features/shop/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'products',
        title: 'Products',
        loadComponent: () =>
          import('./features/shop/catalog/catalog').then((m) => m.Catalog),
      },
      {
        path: 'cart',
        title: 'Cart',
        loadComponent: () => import('./features/shop/cart/cart').then((m) => m.Cart),
      },
      {
        path: 'orders',
        title: 'My orders',
        loadComponent: () =>
          import('./features/shop/my-orders/my-orders').then((m) => m.MyOrders),
      },
      {
        path: 'orders/:id',
        title: 'Order details',
        loadComponent: () =>
          import('./features/orders/order-detail/order-detail').then((m) => m.OrderDetail),
      },
      {
        path: 'notifications',
        title: 'Notifications',
        loadComponent: () =>
          import('./features/notifications/notification-list/notification-list').then(
            (m) => m.NotificationList,
          ),
      },
      {
        path: 'account',
        title: 'Account settings',
        loadComponent: () => import('./features/account/account').then((m) => m.Account),
      },
    ],
  },

  // Public 404, so an unknown URL does not send visitors to the sign-in page
  {
    path: '**',
    title: 'Page not found',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
  },
];
