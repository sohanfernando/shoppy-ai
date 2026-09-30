import { DatePipe, DecimalPipe, Location } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getErrorMessage, isNotFound } from '../../../core/http-error';
import { Order } from '../../../core/models';
import { OrderService } from '../../../core/services/order.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Alert } from '../../../shared/alert';
import { MoneyPipe } from '../../../shared/money';
import { ProductImage } from '../../../shared/product-image';
import { StatusBadge } from '../../../shared/status-badge';
import { UtcDatePipe } from '../../../shared/utc-date.pipe';

@Component({
  selector: 'app-order-detail',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    Alert,
    MoneyPipe,
    ProductImage,
    StatusBadge,
    UtcDatePipe,
  ],
  templateUrl: './order-detail.html',
})
export class OrderDetail {
  private readonly orderService = inject(OrderService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  // Bound from the :id route parameter
  readonly id = input.required<string>();

  protected readonly order = signal<Order | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly cancelling = signal(false);
  protected readonly actionError = signal<string | null>(null);
  private readonly reloadKey = signal(0);

  // Set when arriving from the "New order" page
  protected readonly successMessage = signal<string | null>(
    (inject(Location).getState() as { created?: boolean } | null)?.created
      ? 'Order placed successfully.'
      : null,
  );

  protected readonly isAdmin = this.auth.isAdmin;

  // Admins came from the admin order list, customers from their own
  protected readonly backLink = computed(() => (this.isAdmin() ? '/admin/orders' : '/orders'));

  protected readonly discountPercent = computed(() => {
    const order = this.order();

    return order && order.subtotal > 0 ? (order.discountAmount / order.subtotal) * 100 : 0;
  });

  constructor() {
    effect((onCleanup) => {
      const id = Number(this.id());
      this.reloadKey();

      this.loading.set(true);
      this.notFound.set(false);
      this.error.set(null);

      if (!Number.isInteger(id) || id <= 0) {
        this.notFound.set(true);
        this.loading.set(false);
        return;
      }

      const subscription = this.orderService.getOrder(id).subscribe({
        next: (order) => {
          this.order.set(order);
          this.loading.set(false);
        },
        error: (error) => {
          if (isNotFound(error)) {
            this.notFound.set(true);
          } else {
            this.error.set(getErrorMessage(error));
          }

          this.loading.set(false);
        },
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected retry(): void {
    this.reloadKey.update((key) => key + 1);
  }

  protected cancelOrder(): void {
    const order = this.order();

    if (!order || this.cancelling()) {
      return;
    }

    const confirmed = confirm(
      `Cancel order #${order.id}? The items will be returned to stock. This cannot be undone.`,
    );

    if (!confirmed) {
      return;
    }

    this.cancelling.set(true);
    this.actionError.set(null);
    this.successMessage.set(null);

    this.orderService
      .cancelOrder(order.id)
      .pipe(
        finalize(() => this.cancelling.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.order.set(updated);
          this.successMessage.set('Order cancelled. The items have been returned to stock.');
        },
        error: (error) => {
          this.actionError.set(getErrorMessage(error));
          // The order may have been cancelled elsewhere, so show its current state
          this.retry();
        },
      });
  }
}
