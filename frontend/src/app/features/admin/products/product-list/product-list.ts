import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, finalize, map } from 'rxjs';
import { getErrorMessage } from '../../../../core/http-error';
import { PRODUCT_CATEGORIES, Product, ProductCategory } from '../../../../core/models';
import { ProductService } from '../../../../core/services/product.service';
import { Alert } from '../../../../shared/alert';
import { notBlank, showError, wholeNumber } from '../../../../shared/form-validators';
import { MoneyPipe } from '../../../../shared/money';
import { ProductImage } from '../../../../shared/product-image';

// Must match the limits in the backend's ProductService
const NAME_MAX_LENGTH = 150;
const SKU_MAX_LENGTH = 50;

const LOW_STOCK_THRESHOLD = 5;

@Component({
  selector: 'app-product-list',
  imports: [ReactiveFormsModule, Alert, MoneyPipe, ProductImage],
  templateUrl: './product-list.html',
})
export class ProductList {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly productService = inject(ProductService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly nameMaxLength = NAME_MAX_LENGTH;
  protected readonly skuMaxLength = SKU_MAX_LENGTH;
  protected readonly lowStockThreshold = LOW_STOCK_THRESHOLD;
  protected readonly showError = showError;
  protected readonly categories = PRODUCT_CATEGORIES;

  protected readonly search = signal('');
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
  private readonly reloadKey = signal(0);

  protected readonly showCreateForm = signal(false);
  protected readonly creating = signal(false);
  protected readonly createError = signal<string | null>(null);
  protected readonly createSubmitted = signal(false);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly createForm = this.fb.group({
    name: ['', [Validators.required, notBlank, Validators.maxLength(NAME_MAX_LENGTH)]],
    sku: ['', [Validators.required, notBlank, Validators.maxLength(SKU_MAX_LENGTH)]],
    category: this.fb.control<ProductCategory | ''>('', Validators.required),
    imageUrl: [''],
    unitPrice: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.01)]),
    stock: this.fb.control<number | null>(0, [Validators.required, Validators.min(0), wholeNumber]),
  });

  constructor() {
    effect((onCleanup) => {
      const search = this.debouncedSearch();
      this.reloadKey();

      this.loading.set(true);
      this.error.set(null);

      const subscription = this.productService.getProducts(search).subscribe({
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

  protected openCreateForm(): void {
    this.successMessage.set(null);
    this.showCreateForm.set(true);
  }

  protected closeCreateForm(): void {
    this.showCreateForm.set(false);
    this.createError.set(null);
    this.createSubmitted.set(false);
    this.createForm.reset();
  }

  protected createProduct(): void {
    this.createSubmitted.set(true);
    this.createError.set(null);

    if (this.createForm.invalid || this.creating()) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();

    this.creating.set(true);

    this.productService
      .createProduct({
        name: value.name.trim(),
        sku: value.sku.trim(),
        category: value.category as ProductCategory,
        imageUrl: value.imageUrl.trim() || null,
        unitPrice: Number(value.unitPrice),
        stock: Number(value.stock),
      })
      .pipe(
        finalize(() => this.creating.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (product) => {
          this.closeCreateForm();
          this.successMessage.set(`Product "${product.name}" (${product.sku}) was created.`);
          this.retry();
        },
        error: (error) => this.createError.set(getErrorMessage(error)),
      });
  }
}
