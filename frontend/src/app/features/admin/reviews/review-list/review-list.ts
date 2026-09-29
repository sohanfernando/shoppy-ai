import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { getErrorMessage } from '../../../../core/http-error';
import { PagedResponse, Review } from '../../../../core/models';
import { NotificationService } from '../../../../core/services/notification.service';
import { ReviewService } from '../../../../core/services/review.service';
import { Alert } from '../../../../shared/alert';
import { UtcDatePipe } from '../../../../shared/utc-date.pipe';

const PAGE_SIZE = 20;
const RATINGS = [5, 4, 3, 2, 1];

@Component({
  selector: 'app-review-list',
  imports: [DatePipe, Alert, UtcDatePipe],
  templateUrl: './review-list.html',
})
export class ReviewList {
  private readonly reviewService = inject(ReviewService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly ratings = RATINGS;

  private readonly queryParams = toSignal(this.route.queryParamMap, { requireSync: true });

  protected readonly rating = computed(() => {
    const parsed = Number(this.queryParams().get('rating'));

    return RATINGS.includes(parsed) ? parsed : null;
  });

  protected readonly page = computed(() => {
    const parsed = Number(this.queryParams().get('page'));

    return Number.isInteger(parsed) && parsed > 0 ? parsed : 1;
  });

  protected readonly result = signal<PagedResponse<Review> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  private readonly reloadKey = signal(0);

  constructor() {
    effect((onCleanup) => {
      const rating = this.rating();
      const page = this.page();
      this.reloadKey();

      this.loading.set(true);
      this.error.set(null);

      const subscription = this.reviewService
        .getAll(null, rating, page, PAGE_SIZE)
        .subscribe({
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

    // A review posted right now arrives over the socket, so refresh the list
    effect(() => {
      const toast = this.notifications.toast();

      if (toast?.type === 'review-posted') {
        this.reloadKey.update((key) => key + 1);
      }
    });
  }

  protected onRatingChange(value: string): void {
    this.updateQuery({ rating: value || null, page: null });
  }

  protected goToPage(page: number): void {
    this.updateQuery({ page: page > 1 ? page : null });
  }

  protected retry(): void {
    this.reloadKey.update((key) => key + 1);
  }

  protected stars(rating: number): string {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  }

  protected deleteReview(review: Review): void {
    const confirmed = confirm(
      `Delete the review by ${review.customerName} for ${review.productName}?`,
    );

    if (!confirmed) {
      return;
    }

    this.deletingId.set(review.id);

    this.reviewService
      .delete(review.id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.retry(),
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }

  private updateQuery(queryParams: Params): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
    });
  }
}
