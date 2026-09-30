import { Component, input, signal } from '@angular/core';
import { LucidePackage } from '@lucide/angular';

// Renders a product photo with a neutral fallback tile when there's no
// image or it fails to load — used by both portals, so it stays theme-neutral.
@Component({
  selector: 'app-product-image',
  imports: [LucidePackage],
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
        class="grid size-full place-items-center rounded-lg border border-stone-200 bg-stone-50 text-stone-300"
        [attr.aria-label]="alt()"
      >
        <svg lucidePackage class="size-1/3"></svg>
      </div>
    }
  `,
})
export class ProductImage {
  readonly src = input<string | null>(null);
  readonly alt = input('');

  protected readonly failed = signal(false);
}
