import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';
import {
  CreateOrderRequest,
  Order,
  OrderQuery,
  OrderStatus,
  OrderSummary,
  PagedResponse,
} from '../models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/orders`;

  getOrders(query: OrderQuery): Observable<PagedResponse<OrderSummary>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.status) {
      params = params.set('status', query.status);
    }

    if (query.customerId !== null) {
      params = params.set('customerId', query.customerId);
    }

    return this.http.get<PagedResponse<OrderSummary>>(this.baseUrl, { params });
  }

  // The signed-in customer's own orders
  getMyOrders(
    status: OrderStatus | null,
    page: number,
    pageSize: number,
  ): Observable<PagedResponse<OrderSummary>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PagedResponse<OrderSummary>>(`${this.baseUrl}/my`, { params });
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/${id}`);
  }

  createOrder(request: CreateOrderRequest): Observable<Order> {
    return this.http.post<Order>(this.baseUrl, request);
  }

  cancelOrder(id: number): Observable<Order> {
    return this.http.patch<Order>(`${this.baseUrl}/${id}/cancel`, null);
  }
}
