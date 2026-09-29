import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getErrorMessage } from '../../../core/http-error';
import { CustomerDashboard } from '../../../core/models';
import { DashboardService } from '../../../core/services/dashboard.service';
import { Alert } from '../../../shared/alert';
import { ChartCanvas } from '../../../shared/chart-canvas';
import { monthlySpendChart, spendByProductChart } from '../../../shared/chart-theme';
import { MoneyPipe } from '../../../shared/money';
import { StatusBadge } from '../../../shared/status-badge';
import { UtcDatePipe } from '../../../shared/utc-date.pipe';
import { DatePipe } from '@angular/common';

const MONTH_FORMAT = new Intl.DateTimeFormat('en-US', { month: 'short' });

@Component({
  selector: 'app-dashboard',
  imports: [
    DatePipe,
    RouterLink,
    Alert,
    ChartCanvas,
    MoneyPipe,
    StatusBadge,
    UtcDatePipe,
  ],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly dashboardService = inject(DashboardService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly data = signal<CustomerDashboard | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly hasSpend = computed(() =>
    this.data()?.monthlySpend.some((point) => point.total > 0),
  );

  protected readonly monthlyChart = computed(() => {
    const points = this.data()?.monthlySpend ?? [];

    return monthlySpendChart(
      points.map((point) => MONTH_FORMAT.format(new Date(point.month))),
      points.map((point) => point.total),
    );
  });

  protected readonly productChart = computed(() => {
    const slices = this.data()?.spendByProduct ?? [];

    return spendByProductChart(
      slices.map((slice) => slice.productName),
      slices.map((slice) => slice.total),
    );
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService
      .getCustomerDashboard()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (data) => this.data.set(data),
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }
}
