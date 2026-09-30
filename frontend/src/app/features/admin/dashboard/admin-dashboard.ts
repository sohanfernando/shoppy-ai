import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { getErrorMessage } from '../../../core/http-error';
import { AdminDashboard as AdminDashboardData } from '../../../core/models';
import { DashboardService } from '../../../core/services/dashboard.service';
import { Alert } from '../../../shared/alert';
import { ChartCanvas } from '../../../shared/chart-canvas';
import { spendByProductChart } from '../../../shared/chart-theme';
import { MoneyPipe } from '../../../shared/money';

@Component({
  selector: 'app-admin-dashboard',
  imports: [Alert, ChartCanvas, MoneyPipe],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard {
  private readonly dashboardService = inject(DashboardService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly data = signal<AdminDashboardData | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly topSellersChart = computed(() => {
    const products = this.data()?.topSellingProducts ?? [];

    return spendByProductChart(
      products.map((product) => product.productName),
      products.map((product) => product.revenue),
    );
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService
      .getAdminDashboard()
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
