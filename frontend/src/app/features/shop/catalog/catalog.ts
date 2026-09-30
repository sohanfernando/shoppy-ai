import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, map } from 'rxjs';
import { getErrorMessage } from '../../../core/http-error';
import { PRODUCT_CATEGORIES, Product, ProductReviews } from '../../../core/models';
import { CartService } from '../../../core/services/cart.service';
import { ProductService } from '../../../core/services/product.service';
import { ReviewService } from '../../../core/services/review.service';
import { Alert } from '../../../shared/alert';
import { showError } from '../../../shared/form-validators';
import { MoneyPipe } from '../../../shared/money';
import { ProductImage } from '../../../shared/product-image';
import { DatePipe } from '@angular/common';
import { UtcDatePipe } from '../../../shared/utc-date.pipe';

const RATINGS = [5, 4, 3, 2, 1];

@Component({
  selector: 'app-catalog',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, Alert, MoneyPipe, UtcDatePipe, ProductImage],
  templateUrl: './catalog.html',
})
export class Catalog {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly productService = inject(ProductService);
  private readonly reviewService = inject(ReviewService);
  private readonly cart = inject(CartService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly showError = showError;
  protected readonly ratings = RATINGS;
  protected readonly categories = PRODUCT_CATEGORIES;

  protected readonly search = signal('');
  protected readonly category = signal('');
  private readonly debouncedSearch = toSignal(
    toObservable(this.search).pipe(
      debounceTime(300),
      map((value) => value.trim()),
      distinctUntilChanged(),
    ),
    { initialValue: '' },
  );

  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly addedProductId = signal<number | null>(null);
  private readonly reloadKey = signal(0);

  // Reviews panel for one product at a time
  protected readonly openProduct = signal<Product | null>(null);
  protected readonly reviews = signal<ProductReviews | null>(null);
  protected readonly reviewsLoading = signal(false);
  protected readonly reviewError = signal<string | null>(null);
  protected readonly reviewSubmitted = signal(false);
  protected readonly savingReview = signal(false);

  protected readonly reviewForm = this.fb.group({
    rating: [5, Validators.required],
    comment: ['', [Validators.required, Validators.maxLength(1000)]],
  });

  protected readonly cartCount = this.cart.itemCount;

  constructor() {
    effect((onCleanup) => {
      const search = this.debouncedSearch();
      const category = this.category();
      this.reloadKey();

      this.loading.set(true);
      this.error.set(null);

      const subscription = this.productService.getProducts(search, category).subscribe({
        next: (products) => {
          this.products.set(products);
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

  protected retry(): void {
    this.reloadKey.update((key) => key + 1);
  }

  protected addToCart(product: Product): void {
    this.cart.add(product);
    this.addedProductId.set(product.id);

    setTimeout(() => {
      if (this.addedProductId() === product.id) {
        this.addedProductId.set(null);
      }
    }, 2000);
  }

  // ---- Reviews ----

  protected openReviews(product: Product): void {
    this.openProduct.set(product);
    this.reviews.set(null);
    this.reviewError.set(null);
    this.reviewSubmitted.set(false);
    this.reviewForm.reset({ rating: 5, comment: '' });
    this.loadReviews(product.id);
  }

  protected closeReviews(): void {
    this.openProduct.set(null);
    this.reviews.set(null);
  }

  protected submitReview(): void {
    const product = this.openProduct();

    this.reviewSubmitted.set(true);
    this.reviewError.set(null);

    if (!product || this.reviewForm.invalid || this.savingReview()) {
      this.reviewForm.markAllAsTouched();
      return;
    }

    const value = this.reviewForm.getRawValue();

    this.savingReview.set(true);

    this.reviewService
      .create({
        productId: product.id,
        rating: Number(value.rating),
        comment: value.comment.trim(),
      })
      .pipe(
        finalize(() => this.savingReview.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.reviewForm.reset({ rating: 5, comment: '' });
          this.reviewSubmitted.set(false);
          this.loadReviews(product.id);
        },
        error: (error) => this.reviewError.set(getErrorMessage(error)),
      });
  }

  protected stars(rating: number): string {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  }

  private loadReviews(productId: number): void {
    this.reviewsLoading.set(true);

    this.reviewService
      .getForProduct(productId)
      .pipe(
        finalize(() => this.reviewsLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (reviews) => this.reviews.set(reviews),
        error: (error) => this.reviewError.set(getErrorMessage(error)),
      });
  }
}
