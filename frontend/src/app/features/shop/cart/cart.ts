import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { getErrorMessage } from '../../../core/http-error';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { Alert } from '../../../shared/alert';
import { MoneyPipe, roundMoney } from '../../../shared/money';
import { ProductImage } from '../../../shared/product-image';

@Component({
  selector: 'app-cart',
  imports: [RouterLink, Alert, MoneyPipe, ProductImage],
  templateUrl: './cart.html',
})
export class Cart {
  private readonly cart = inject(CartService);
  private readonly auth = inject(AuthService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private pendingCheckout: { fingerprint: string; id: string } | null = null;

  protected readonly lines = this.cart.lines;
  protected readonly itemCount = this.cart.itemCount;
  protected readonly subtotal = this.cart.subtotal;
  protected readonly hasStockProblem = this.cart.hasStockProblem;

  protected readonly placing = signal(false);
  protected readonly error = signal<string | null>(null);

  // Customers pay the full price; discounts stay an admin decision
  protected readonly total = computed(() => roundMoney(this.subtotal()));

  protected setQuantity(productId: number, value: string): void {
    this.cart.setQuantity(productId, Number(value));
    if (this.lines().length === 0) {
      this.discardCheckout();
    }
  }

  protected remove(productId: number): void {
    this.cart.remove(productId);
    if (this.lines().length === 0) {
      this.discardCheckout();
    }
  }

  protected clear(): void {
    this.cart.clear();
    this.discardCheckout();
  }

  protected checkout(): void {
    const user = this.auth.user();
    if (!user || this.lines().length === 0 || this.placing() || this.hasStockProblem()) {
      return;
    }

    const items = this.checkoutItems();
    const fingerprint = JSON.stringify(items);
    const storageKey = `aos.checkout.${user.id}`;
    const checkoutId = this.getCheckoutId(storageKey, fingerprint);
    this.placing.set(true);
    this.error.set(null);

    this.orderService
      .createOrder({ checkoutId, items })
      .pipe(
        finalize(() => this.placing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (order) => {
          if (this.pendingCheckout?.id === checkoutId) {
            this.discardCheckout();
            this.cart.removePurchasedItems(items);
          }
          this.router.navigate(['/orders', order.id], { state: { created: true } });
        },
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }

  private discardCheckout(): void {
    this.pendingCheckout = null;
    const user = this.auth.user();
    if (user) {
      try {
        sessionStorage.removeItem(`aos.checkout.${user.id}`);
      } catch {
        // Checkout also works when browser storage is blocked.
      }
    }
  }

  private checkoutItems() {
    return this.lines()
      .map(({ productId, quantity }) => ({ productId, quantity }))
      .sort((a, b) => a.productId - b.productId);
  }

  private getCheckoutId(storageKey: string, fingerprint: string): string {
    let stored = this.pendingCheckout;
    try {
      stored ??= JSON.parse(sessionStorage.getItem(storageKey) ?? 'null');
    } catch {
      // Keep the retry ID in memory when storage is unavailable.
    }
    const id = stored?.fingerprint === fingerprint && typeof stored.id === 'string'
      ? stored.id
      : crypto.randomUUID();
    this.pendingCheckout = { fingerprint, id };
    try {
      sessionStorage.setItem(storageKey, JSON.stringify(this.pendingCheckout));
    } catch {
      // The in-memory ID still covers retries on this page.
    }
    return id;
  }
}
