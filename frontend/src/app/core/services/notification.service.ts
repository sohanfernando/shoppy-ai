import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { API_BASE_URL, NOTIFICATIONS_HUB_URL } from '../config';
import { AppNotification, NotificationList } from '../models';

const TOAST_MILLISECONDS = 6000;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/notifications`;

  private connection: HubConnection | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly notifications = signal<AppNotification[]>([]);

  readonly items = this.notifications.asReadonly();
  readonly unreadCount = computed(
    () => this.notifications().filter((notification) => !notification.isRead).length,
  );

  // The most recent live arrival, shown as a toast for a few seconds
  readonly toast = signal<AppNotification | null>(null);

  load(): void {
    this.http.get<NotificationList>(this.baseUrl).subscribe({
      next: (response) => this.notifications.set(response.items),
      // The bell simply stays empty if this fails
      error: () => this.notifications.set([]),
    });
  }

  // Opens the live connection; safe to call more than once
  async connect(): Promise<void> {
    if (this.connection) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(NOTIFICATIONS_HUB_URL, { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('notification', (notification: AppNotification) => {
      this.notifications.update((current) => [notification, ...current].slice(0, 30));
      this.showToast(notification);
    });

    try {
      await this.connection.start();
    } catch {
      // Without the socket the bell still works, it just needs a page refresh
      this.connection = null;
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.notifications.set([]);
    this.clearToast();

    await connection?.stop();
  }

  markAsRead(notification: AppNotification): void {
    if (notification.isRead) {
      return;
    }

    this.patchRead([notification.id]);

    this.http.post<void>(`${this.baseUrl}/${notification.id}/read`, null).subscribe({
      error: () => this.load(),
    });
  }

  markAllAsRead(): void {
    const unreadIds = this.notifications()
      .filter((notification) => !notification.isRead)
      .map((notification) => notification.id);

    if (unreadIds.length === 0) {
      return;
    }

    this.patchRead(unreadIds);

    this.http.post<void>(`${this.baseUrl}/read-all`, null).subscribe({
      error: () => this.load(),
    });
  }

  clearToast(): void {
    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
      this.toastTimer = null;
    }

    this.toast.set(null);
  }

  private showToast(notification: AppNotification): void {
    this.clearToast();
    this.toast.set(notification);
    this.toastTimer = setTimeout(() => this.toast.set(null), TOAST_MILLISECONDS);
  }

  private patchRead(ids: number[]): void {
    this.notifications.update((current) =>
      current.map((notification) =>
        ids.includes(notification.id) ? { ...notification, isRead: true } : notification,
      ),
    );
  }
}
