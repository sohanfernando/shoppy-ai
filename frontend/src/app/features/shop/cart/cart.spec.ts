import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
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
    sessionStorage.removeItem('aos.checkout.1');
    response = new Subject<Order>();
    createOrder.mockClear();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { user: () => ({ id: 1 }) } },
        { provide: OrderService, useValue: { createOrder } },
        { provide: Router, useValue: { navigate: vi.fn() } },
      ],
    });
    cart = TestBed.inject(CartService);
  });

  it('subtracts submitted quantities and preserves later additions', () => {
    cart.add(product(1));
    cart.add(product(2));
    const page = openCart();
    page.checkout();
    cart.add(product(1), 2);
    page.remove(2);
    cart.add(product(3));

    response.next({ id: 1 } as Order);

    expect(cart.lines().map(({ productId, quantity }) => [productId, quantity]))
      .toEqual([[1, 2], [3, 1]]);
  });

  it.each(['clear', 'remove'] as const)('starts a new checkout after %s empties the cart', (action) => {
    cart.add(product(1));
    const page = openCart();
    page.checkout();
    response.error(new Error('Connection lost'));
    response = new Subject<Order>();
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
