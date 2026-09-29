import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AppNotification } from '../../../core/models';
import { NotificationService } from '../../../core/services/notification.service';
import { UtcDatePipe } from '../../../shared/utc-date.pipe';

@Component({
  selector: 'app-notification-list',
  imports: [DatePipe, UtcDatePipe],
  templateUrl: './notification-list.html',
})
export class NotificationList {
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly items = this.notificationService.items;
  protected readonly unreadCount = this.notificationService.unreadCount;

  constructor() {
    this.notificationService.load();
  }

  protected open(notification: AppNotification): void {
    this.notificationService.markAsRead(notification);

    if (notification.link) {
      this.router.navigateByUrl(notification.link);
    }
  }

  protected markAllAsRead(): void {
    this.notificationService.markAllAsRead();
  }
}
