import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subscription, retry, throwError, timer } from 'rxjs';
import { API_BASE_URL, NOTIFICATIONS_HUB_URL } from '../config';
import { AppNotification, NotificationList } from '../models';

const TOAST_MILLISECONDS = 6000;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/notifications`;

  private connection: HubConnection | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;
  private loadRequest?: Subscription;
  private sessionVersion = 0;

  private readonly notifications = signal<AppNotification[]>([]);
  private readonly totalUnread = signal(0);

  readonly items = this.notifications.asReadonly();
  readonly unreadCount = this.totalUnread.asReadonly();

  // The most recent live arrival, shown as a toast for a few seconds
  readonly toast = signal<AppNotification | null>(null);

  load(): void {
    this.loadRequest?.unsubscribe();
    this.loadRequest = this.http.get<NotificationList>(this.baseUrl).pipe(
      retry({
        count: 2,
        delay: (error, attempt) =>
          error.status === 0 || error.status >= 500
            ? timer(attempt * 500)
            : throwError(() => error),
      }),
    ).subscribe({
      next: (response) => {
        this.notifications.set(response.items);
        this.totalUnread.set(response.unreadCount);
      },
      error: () => {},
    });
  }

  // Opens the live connection; safe to call more than once
  async connect(): Promise<void> {
    if (this.connection) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(NOTIFICATIONS_HUB_URL, { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection = connection;

    connection.on('notification', (notification: AppNotification) => {
      if (this.connection === connection) {
        this.showToast(notification);
        this.load();
      }
    });
    connection.onreconnected(() => {
      if (this.connection === connection) {
        this.load();
      }
    });

    try {
      await connection.start();
    } catch {
      // Without the socket the bell still works, it just needs a page refresh
      if (this.connection === connection) {
        this.connection = null;
      }
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.sessionVersion++;
    this.loadRequest?.unsubscribe();
    this.notifications.set([]);
    this.totalUnread.set(0);
    this.clearToast();

    await connection?.stop();
  }

  markAsRead(notification: AppNotification): void {
    if (notification.isRead) {
      return;
    }

    this.markRead(notification.id);
  }

  markAllAsRead(): void {
    if (this.totalUnread() > 0) {
      this.markRead();
    }
  }

  private markRead(id?: number): void {
    const path = id === undefined ? 'read-all' : `${id}/read`;
    const version = this.sessionVersion;
    this.http.post<void>(`${this.baseUrl}/${path}`, null).subscribe({
      next: () => {
        if (version === this.sessionVersion) {
          if (id === undefined) {
            this.notifications.update((items) => items.map((item) => ({ ...item, isRead: true })));
            this.totalUnread.set(0);
          } else if (this.notifications().some((item) => item.id === id && !item.isRead)) {
            this.notifications.update((items) =>
              items.map((item) => item.id === id ? { ...item, isRead: true } : item),
            );
            this.totalUnread.update((count) => Math.max(0, count - 1));
          }
          this.load();
        }
      },
      error: () => {},
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

}
