import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';
import { CreateReviewRequest, PagedResponse, ProductReviews, Review } from '../models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/reviews`;

  getForProduct(productId: number): Observable<ProductReviews> {
    return this.http.get<ProductReviews>(`${this.baseUrl}/product/${productId}`);
  }

  create(request: CreateReviewRequest): Observable<Review> {
    return this.http.post<Review>(this.baseUrl, request);
  }

  // Admin reviews page
  getAll(
    productId: number | null,
    rating: number | null,
    page: number,
    pageSize: number,
  ): Observable<PagedResponse<Review>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (productId !== null) {
      params = params.set('productId', productId);
    }

    if (rating !== null) {
      params = params.set('rating', rating);
    }

    return this.http.get<PagedResponse<Review>>(this.baseUrl, { params });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
