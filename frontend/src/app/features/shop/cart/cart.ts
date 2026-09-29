import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getErrorMessage } from '../../../core/http-error';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { Alert } from '../../../shared/alert';
import { MoneyPipe, roundMoney } from '../../../shared/money';

@Component({
  selector: 'app-cart',
  imports: [RouterLink, Alert, MoneyPipe],
  templateUrl: './cart.html',
})
export class Cart {
  private readonly cart = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

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
  }

  protected remove(productId: number): void {
    this.cart.remove(productId);
  }

  protected clear(): void {
    this.cart.clear();
  }

  protected checkout(): void {
    if (this.lines().length === 0 || this.placing() || this.hasStockProblem()) {
      return;
    }

    this.placing.set(true);
    this.error.set(null);

    this.orderService
      .createOrder({
        discountPercent: 0,
        items: this.lines().map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
        })),
      })
      .pipe(
        finalize(() => this.placing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (order) => {
          this.cart.clear();
          this.router.navigate(['/orders', order.id], { state: { created: true } });
        },
        error: (error) => this.error.set(getErrorMessage(error)),
      });
  }
}
