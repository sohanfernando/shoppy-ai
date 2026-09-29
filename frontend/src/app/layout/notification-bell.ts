import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../core/services/notification.service';
import { AppNotification } from '../core/models';
import { UtcDatePipe } from '../shared/utc-date.pipe';

@Component({
  selector: 'app-notification-bell',
  imports: [DatePipe, UtcDatePipe],
  template: `
    <div class="relative">
      <button
        type="button"
        class="relative rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
        [attr.aria-expanded]="open()"
        aria-haspopup="true"
        (click)="toggle()"
      >
        <span class="sr-only">Notifications</span>
        <svg
          class="size-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.8"
          aria-hidden="true"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M15 17h5l-1.4-1.4A2 2 0 0 1 18 14.2V11a6 6 0 1 0-12 0v3.2c0 .5-.2 1-.6 1.4L4 17h5m6 0a3 3 0 1 1-6 0m6 0H9"
          />
        </svg>
        @if (unreadCount() > 0) {
          <span
            class="absolute top-1 right-1 grid min-w-4 place-items-center rounded-full bg-rose-600 px-1 text-[10px] font-semibold text-white"
          >
            {{ unreadCount() > 9 ? '9+' : unreadCount() }}
          </span>
        }
      </button>

      @if (open()) {
        <!-- Click-away layer -->
        <button
          type="button"
          class="fixed inset-0 z-10 cursor-default"
          tabindex="-1"
          aria-label="Close notifications"
          (click)="close()"
        ></button>

        <div
          class="absolute right-0 z-20 mt-2 w-80 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-lg"
        >
          <div class="flex items-center justify-between border-b border-slate-200 px-4 py-3">
            <h2 class="text-sm font-semibold text-slate-900">Notifications</h2>
            @if (unreadCount() > 0) {
              <button type="button" class="link text-xs" (click)="markAllAsRead()">
                Mark all read
              </button>
            }
          </div>

          <ul class="max-h-96 divide-y divide-slate-100 overflow-y-auto">
            @for (notification of items(); track notification.id) {
              <li>
                <button
                  type="button"
                  class="block w-full px-4 py-3 text-left hover:bg-slate-50"
                  [class.bg-indigo-50/40]="!notification.isRead"
                  (click)="openNotification(notification)"
                >
                  <span class="flex items-start gap-2">
                    @if (!notification.isRead) {
                      <span
                        class="mt-1.5 size-2 shrink-0 rounded-full bg-indigo-600"
                        aria-hidden="true"
                      ></span>
                    }
                    <span class="min-w-0">
                      <span class="block text-sm font-medium text-slate-900">
                        {{ notification.title }}
                      </span>
                      <span class="mt-0.5 block text-xs text-slate-600">
                        {{ notification.message }}
                      </span>
                      <span class="mt-1 block text-xs text-slate-400">
                        {{ notification.createdAt | utcDate | date: 'short' }}
                      </span>
                    </span>
                  </span>
                </button>
              </li>
            } @empty {
              <li class="px-4 py-8 text-center text-sm text-slate-500">Nothing yet.</li>
            }
          </ul>
        </div>
      }
    </div>
  `,
})
export class NotificationBell {
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly items = this.notificationService.items;
  protected readonly unreadCount = this.notificationService.unreadCount;
  protected readonly open = signal(false);

  protected toggle(): void {
    this.open.update((isOpen) => !isOpen);
  }

  protected close(): void {
    this.open.set(false);
  }

  protected markAllAsRead(): void {
    this.notificationService.markAllAsRead();
  }

  protected openNotification(notification: AppNotification): void {
    this.notificationService.markAsRead(notification);
    this.close();

    if (notification.link) {
      this.router.navigateByUrl(notification.link);
    }
  }
}
