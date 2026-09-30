import { Injectable, computed, signal } from '@angular/core';
import { CartLine, Product } from '../models';
import { roundMoney } from '../../shared/money';

const STORAGE_KEY = 'aos.cart';

// The cart lives in the browser until the order is placed
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly cartLines = signal<CartLine[]>(readStoredCart());

  readonly lines = this.cartLines.asReadonly();

  readonly itemCount = computed(() =>
    this.cartLines().reduce((sum, line) => sum + line.quantity, 0),
  );

  readonly subtotal = computed(() =>
    roundMoney(this.cartLines().reduce((sum, line) => sum + line.unitPrice * line.quantity, 0)),
  );

  readonly hasStockProblem = computed(() =>
    this.cartLines().some((line) => line.quantity > line.stock),
  );

  add(product: Product, quantity = 1): void {
    this.cartLines.update((lines) => {
      const existing = lines.find((line) => line.productId === product.id);

      if (existing) {
        return lines.map((line) =>
          line.productId === product.id
            ? {
                ...line,
                // Never put more in the cart than the shop has
                quantity: Math.min(line.quantity + quantity, product.stock),
                unitPrice: product.unitPrice,
                stock: product.stock,
                imageUrl: product.imageUrl,
              }
            : line,
        );
      }

      return [
        ...lines,
        {
          productId: product.id,
          name: product.name,
          sku: product.sku,
          imageUrl: product.imageUrl,
          unitPrice: product.unitPrice,
          quantity: Math.min(quantity, product.stock),
          stock: product.stock,
        },
      ];
    });

    this.persist();
  }

  setQuantity(productId: number, quantity: number): void {
    if (quantity < 1) {
      this.remove(productId);
      return;
    }

    this.cartLines.update((lines) =>
      lines.map((line) =>
        line.productId === productId
          ? { ...line, quantity: Math.min(quantity, line.stock) }
          : line,
      ),
    );

    this.persist();
  }

  remove(productId: number): void {
    this.cartLines.update((lines) => lines.filter((line) => line.productId !== productId));
    this.persist();
  }

  clear(): void {
    this.cartLines.set([]);
    this.persist();
  }

  private persist(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.cartLines()));
    } catch {
      // A full or blocked storage only costs us the saved cart
    }
  }
}

function readStoredCart(): CartLine[] {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    const parsed = stored ? JSON.parse(stored) : null;

    return Array.isArray(parsed) ? (parsed as CartLine[]) : [];
  } catch {
    return [];
  }
}
