import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { getErrorMessage } from '../../../../core/http-error';
import { Customer } from '../../../../core/models';
import { CustomerService } from '../../../../core/services/customer.service';
import { Alert } from '../../../../shared/alert';
import { UtcDatePipe } from '../../../../shared/utc-date.pipe';

@Component({
  selector: 'app-customer-list',
  imports: [DatePipe, RouterLink, Alert, UtcDatePipe],
  templateUrl: './customer-list.html',
})
export class CustomerList {
  private readonly customerService = inject(CustomerService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly customers = signal<Customer[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly search = signal('');

  // The customer list is small, so it is filtered in the browser
  protected readonly filteredCustomers = computed(() => {
    const term = this.search().trim().toLowerCase();

    if (!term) {
      return this.customers();
    }

    return this.customers().filter(
      (customer) =>
        customer.name.toLowerCase().includes(term) || customer.email.toLowerCase().includes(term),
    );
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.customerService
      .getCustomers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (customers) => {
          this.customers.set(customers);
          this.loading.set(false);
        },
        error: (error) => {
          this.error.set(getErrorMessage(error));
          this.loading.set(false);
        },
      });
  }
}
