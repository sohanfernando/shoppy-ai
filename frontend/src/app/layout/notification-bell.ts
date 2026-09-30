import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LucideBell } from '@lucide/angular';
import { NotificationService } from '../core/services/notification.service';
import { AppNotification } from '../core/models';
import { UtcDatePipe } from '../shared/utc-date.pipe';

@Component({
  selector: 'app-notification-bell',
  imports: [DatePipe, UtcDatePipe, LucideBell],
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
        <svg lucideBell class="size-5"></svg>
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
                  [class.notif-unread-row]="!notification.isRead"
                  (click)="openNotification(notification)"
                >
                  <span class="flex items-start gap-2">
                    @if (!notification.isRead) {
                      <span
                        class="notif-unread-dot mt-1.5 size-2 shrink-0 rounded-full"
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
