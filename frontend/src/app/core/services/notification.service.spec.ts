import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { API_BASE_URL } from '../config';
import { NotificationService } from './notification.service';

describe('Notification read refresh', () => {
  it('keeps the successful read and retries only the failed refresh', async () => {
    vi.useFakeTimers();
    try {
      TestBed.configureTestingModule({
        providers: [provideHttpClient(), provideHttpClientTesting()],
      });
      const service = TestBed.inject(NotificationService);
      const http = TestBed.inject(HttpTestingController);
      const url = `${API_BASE_URL}/notifications`;
      const notification = {
        id: 1, type: 'OrderPlaced', title: 'Order', message: '', link: '',
        isRead: false, createdAt: '',
      };

      service.load();
      http.expectOne({ method: 'GET', url }).flush({ items: [notification], unreadCount: 2 });
      service.markAllAsRead();
      http.expectOne({ method: 'POST', url: `${url}/read-all` }).flush(null);
      http.expectOne({ method: 'GET', url }).flush(null, { status: 503, statusText: 'Unavailable' });
      expect(service.unreadCount()).toBe(0);
      expect(service.items()[0].isRead).toBe(true);

      await vi.advanceTimersByTimeAsync(500);
      http.expectOne({ method: 'GET', url }).flush({
        items: [{ ...notification, isRead: true }], unreadCount: 0,
      });
      http.verify();
    } finally {
      vi.useRealTimers();
    }
  });
});
