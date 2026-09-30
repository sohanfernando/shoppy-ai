import { Component, input, signal } from '@angular/core';

// Renders a product photo with a neutral fallback tile when there's no
// image or it fails to load — used by both portals, so it stays theme-neutral.
@Component({
  selector: 'app-product-image',
  template: `
    @if (src() && !failed()) {
      <img
        [src]="src()"
        [alt]="alt()"
        class="size-full rounded-lg object-cover"
        (error)="failed.set(true)"
      />
    } @else {
      <div
        class="grid size-full place-items-center rounded-lg bg-slate-100 text-slate-400"
        [attr.aria-label]="alt()"
      >
        <svg class="size-1/3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M3.75 7.5 12 3l8.25 4.5v9L12 21l-8.25-4.5v-9Z"
          />
          <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 7.5 12 12m0 0 8.25-4.5M12 12v9" />
        </svg>
      </div>
    }
  `,
})
export class ProductImage {
  readonly src = input<string | null>(null);
  readonly alt = input('');

  protected readonly failed = signal(false);
}
