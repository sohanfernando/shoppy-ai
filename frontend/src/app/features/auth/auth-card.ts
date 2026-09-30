import { Component, input } from '@angular/core';
import { APP_NAME } from '../../core/config';

@Component({
  selector: 'app-auth-card',
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center px-4 py-12">
      <div class="mb-6 flex items-center gap-2 font-semibold text-slate-900">
        <span
          class="auth-logo-badge grid size-10 place-items-center rounded-lg bg-indigo-600 font-bold text-white"
          aria-hidden="true"
        >
          AO
        </span>
        <span class="text-lg">{{ appName }}</span>
      </div>

      <div class="card w-full max-w-md p-6 sm:p-8">
        <h1 class="text-xl font-semibold text-slate-900">{{ heading() }}</h1>
        @if (subheading()) {
          <p class="mt-1 text-sm text-slate-500">{{ subheading() }}</p>
        }
        <div class="mt-6">
          <ng-content />
        </div>
      </div>

      <div class="mt-6 text-center text-sm text-slate-600">
        <ng-content select="[footer]" />
      </div>
    </div>
  `,
})
export class AuthCard {
  // Not named "title": that would also become a tooltip on the host element
  readonly heading = input.required<string>();
  readonly subheading = input<string>();

  protected readonly appName = APP_NAME;
}
