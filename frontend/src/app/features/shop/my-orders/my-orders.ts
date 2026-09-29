import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { getErrorMessage } from '../../../core/http-error';
import { ORDER_STATUSES, OrderStatus, OrderSummary, PagedResponse } from '../../../core/models';
import { OrderService } from '../../../core/services/order.service';
import { Alert } from '../../../shared/alert';
import { MoneyPipe } from '../../../shared/money';
import { StatusBadge } from '../../../shared/status-badge';
import { UtcDatePipe } from '../../../shared/utc-date.pipe';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-my-orders',
  imports: [DatePipe, RouterLink, Alert, MoneyPipe, StatusBadge, UtcDatePipe],
  templateUrl: './my-orders.html',
})
export class MyOrders {
  private readonly orderService = inject(OrderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly statuses = ORDER_STATUSES;

  private readonly queryParams = toSignal(this.route.queryParamMap, { requireSync: true });

  protected readonly status = computed(
    () => ORDER_STATUSES.find((value) => value === this.queryParams().get('status')) ?? null,
  );

  protected readonly page = computed(() => {
    const parsed = Number(this.queryParams().get('page'));

    return Number.isInteger(parsed) && parsed > 0 ? parsed : 1;
  });

  protected readonly result = signal<PagedResponse<OrderSummary> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  private readonly reloadKey = signal(0);

  constructor() {
    effect((onCleanup) => {
      const status = this.status();
      const page = this.page();
      this.reloadKey();

      this.loading.set(true);
      this.error.set(null);

      const subscription = this.orderService.getMyOrders(status, page, PAGE_SIZE).subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: (error) => {
          this.error.set(getErrorMessage(error));
          this.loading.set(false);
        },
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected onStatusChange(value: string): void {
    this.updateQuery({ status: value || null, page: null });
  }

  protected goToPage(page: number): void {
    this.updateQuery({ page: page > 1 ? page : null });
  }

  protected retry(): void {
    this.reloadKey.update((key) => key + 1);
  }

  private updateQuery(queryParams: Params): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
    });
  }
}
