import {
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

Chart.register(...registerables);

// Thin wrapper so pages describe a chart as data and never touch the canvas
@Component({
  selector: 'app-chart-canvas',
  template: `<canvas #canvas [attr.aria-label]="label()" role="img"></canvas>`,
  host: { class: 'block' },
})
export class ChartCanvas {
  readonly config = input.required<ChartConfiguration>();
  readonly label = input<string>('');

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;

  constructor() {
    effect(() => {
      const config = this.config();
      const element = this.canvas().nativeElement;

      this.chart?.destroy();
      this.chart = new Chart(element, config);
    });

    inject(DestroyRef).onDestroy(() => this.chart?.destroy());
  }
}
