import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { getErrorMessage } from '../../../../core/http-error';
import {
  Customer,
  ORDER_STATUSES,
  OrderQuery,
  OrderStatus,
  OrderSummary,
  PagedResponse,
} from '../../../../core/models';
import { CustomerService } from '../../../../core/services/customer.service';
import { OrderService } from '../../../../core/services/order.service';
import { Alert } from '../../../../shared/alert';
import { MoneyPipe } from '../../../../shared/money';
import { StatusBadge } from '../../../../shared/status-badge';
import { UtcDatePipe } from '../../../../shared/utc-date.pipe';

const PAGE_SIZES = [10, 20, 50];

function parsePositiveInt(value: string | null): number | null {
  const parsed = Number(value);

  return value && Number.isInteger(parsed) && parsed > 0 ? parsed : null;
}

function parseStatus(value: string | null): OrderStatus | null {
  return ORDER_STATUSES.find((status) => status === value) ?? null;
}

@Component({
  selector: 'app-order-list',
  imports: [DatePipe, RouterLink, Alert, MoneyPipe, StatusBadge, UtcDatePipe],
  templateUrl: './order-list.html',
})
export class OrderList {
  private readonly orderService = inject(OrderService);
  private readonly customerService = inject(CustomerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly statuses = ORDER_STATUSES;
  protected readonly pageSizes = PAGE_SIZES;

  // Filters live in the URL so they survive reloads and the back button
  private readonly queryParams = toSignal(this.route.queryParamMap, { requireSync: true });

  protected readonly status = computed(() => parseStatus(this.queryParams().get('status')));
  protected readonly customerId = computed(() =>
    parsePositiveInt(this.queryParams().get('customerId')),
  );
  protected readonly page = computed(() => parsePositiveInt(this.queryParams().get('page')) ?? 1);
  protected readonly pageSize = computed(() => {
    const size = parsePositiveInt(this.queryParams().get('pageSize'));

    return size !== null && PAGE_SIZES.includes(size) ? size : PAGE_SIZES[0];
  });

  protected readonly hasFilters = computed(
    () => this.status() !== null || this.customerId() !== null,
  );

  protected readonly customers = signal<Customer[]>([]);
  protected readonly result = signal<PagedResponse<OrderSummary> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  private readonly reloadKey = signal(0);

  protected readonly rangeLabel = computed(() => {
    const result = this.result();

    if (!result || result.items.length === 0) {
      return '';
    }

    const start = (result.page - 1) * result.pageSize + 1;
    const end = start + result.items.length - 1;

    return `Showing ${start}–${end} of ${result.totalItems}`;
  });

  constructor() {
    // Only used for the customer filter, so a failure here is not shown
    this.customerService
      .getCustomers()
      .pipe(takeUntilDestroyed())
      .subscribe({ next: (customers) => this.customers.set(customers) });

    effect((onCleanup) => {
      const query: OrderQuery = {
        status: this.status(),
        customerId: this.customerId(),
        page: this.page(),
        pageSize: this.pageSize(),
      };
      this.reloadKey();

      this.loading.set(true);
      this.error.set(null);

      const subscription = this.orderService.getOrders(query).subscribe({
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

  protected onCustomerChange(value: string): void {
    this.updateQuery({ customerId: value || null, page: null });
  }

  protected onPageSizeChange(value: string): void {
    this.updateQuery({ pageSize: value, page: null });
  }

  protected goToPage(page: number): void {
    this.updateQuery({ page: page > 1 ? page : null });
  }

  protected clearFilters(): void {
    this.updateQuery({ status: null, customerId: null, page: null });
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
