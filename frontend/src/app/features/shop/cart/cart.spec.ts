import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../../../core/auth/auth.service';
import { CreateOrderRequest, Order, Product } from '../../../core/models';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { Cart } from './cart';

type CartActions = {
  checkout(): void;
  clear(): void;
  remove(productId: number): void;
};

const product = (id: number): Product => ({
  id,
  name: 'Product',
  sku: String(id),
  category: 'Electronics',
  unitPrice: 10,
  stock: 10,
  imageUrl: null,
  isActive: true,
  createdAt: '',
});

describe('Cart checkout', () => {
  let cart: CartService;
  let response: Subject<Order>;
  const createOrder = vi.fn((_: CreateOrderRequest) => response);
  const openCart = () => TestBed.runInInjectionContext(() => new Cart()) as unknown as CartActions;

  beforeEach(() => {
    localStorage.removeItem('aos.cart');
    localStorage.removeItem('aos.checkout.1');
    sessionStorage.removeItem('aos.checkout.1');
    response = new Subject<Order>();
    createOrder.mockClear();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { user: () => ({ id: 1 }) } },
        { provide: OrderService, useValue: { createOrder } },
        provideRouter([]),
      ],
    });
    cart = TestBed.inject(CartService);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  it('subtracts submitted quantities and preserves later additions', () => {
    cart.add(product(1));
    cart.add(product(2));
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();
    const page = fixture.componentInstance as unknown as CartActions;
    page.checkout();
    const quantityInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input[type="number"]',
    )!;
    quantityInput.value = '3';
    quantityInput.dispatchEvent(new Event('change', { bubbles: true }));
    page.remove(2);
    cart.add(product(3));

    response.next({ id: 1 } as Order);

    expect(cart.lines().map(({ productId, quantity }) => [productId, quantity]))
      .toEqual([[1, 2], [3, 1]]);
  });

  it('keeps the cart and starts a fresh checkout after a key conflict', () => {
    cart.add(product(1));
    const page = openCart();
    page.checkout();
    response.error(new HttpErrorResponse({ status: 409 }));
    expect(cart.itemCount()).toBe(1);

    response = new Subject<Order>();
    page.checkout();
    expect(createOrder.mock.calls[1][0].checkoutId).not.toBe(createOrder.mock.calls[0][0].checkoutId);
  });

  it.each(['clear', 'remove'] as const)('starts a new checkout after %s empties the cart', (action) => {
    cart.add(product(1));
    const page = openCart();
    page.checkout();
    response.error(new Error('Connection lost'));
    response = new Subject<Order>();
    sessionStorage.clear();
    const reopened = openCart();
    reopened.checkout();
    expect(createOrder.mock.calls[1][0].checkoutId).toBe(createOrder.mock.calls[0][0].checkoutId);
    response.error(new Error('Connection lost'));

    if (action === 'clear') {
      reopened.clear();
    } else {
      reopened.remove(1);
    }
    cart.add(product(1));
    response = new Subject<Order>();
    openCart().checkout();
    expect(createOrder.mock.calls[2][0].checkoutId).not.toBe(createOrder.mock.calls[0][0].checkoutId);
  });
});
