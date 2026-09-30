import { Component, computed, input } from '@angular/core';

export type AlertType = 'info' | 'success' | 'error' | 'warning';

const ALERT_CLASSES: Record<AlertType, string> = {
  info: 'border-sky-200 bg-sky-50 text-sky-800',
  success: 'border-emerald-200 bg-emerald-50 text-emerald-800',
  error: 'border-rose-200 bg-rose-50 text-rose-800',
  warning: 'border-amber-200 bg-amber-50 text-amber-800',
};

@Component({
  selector: 'app-alert',
  template: `
    <div
      class="rounded-lg border px-4 py-3 text-sm"
      [class]="classes()"
      [attr.role]="type() === 'error' ? 'alert' : 'status'"
    >
      <ng-content />
    </div>
  `,
})
export class Alert {
  readonly type = input<AlertType>('info');

  protected readonly classes = computed(() => ALERT_CLASSES[this.type()]);
}
