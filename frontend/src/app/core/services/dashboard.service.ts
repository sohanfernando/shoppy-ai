import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';
import { CustomerDashboard } from '../models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getCustomerDashboard(): Observable<CustomerDashboard> {
    return this.http.get<CustomerDashboard>(`${API_BASE_URL}/dashboard/customer`);
  }
}
