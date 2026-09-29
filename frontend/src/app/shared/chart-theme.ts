import { Chart, ChartConfiguration, Plugin } from 'chart.js';
import { CURRENCY_SYMBOL } from '../core/config';

// One blue hue. Steps are far enough apart to read as distinct and the lightest
// still clears the surface, checked with the dataviz palette validator.
export const SEQUENTIAL_BLUE = ['#0d366b', '#184f95', '#256abf', '#3987e5', '#86b6ef'];

export const SERIES_BLUE = '#2a78d6';

const GRID_COLOR = '#e2e8f0';
const TEXT_MUTED = '#64748b';

const compactMoney = new Intl.NumberFormat('en-US', {
  notation: 'compact',
  maximumFractionDigits: 1,
});

const fullMoney = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function money(value: number): string {
  return `${CURRENCY_SYMBOL} ${fullMoney.format(value)}`;
}

function axisMoney(value: number): string {
  return `${CURRENCY_SYMBOL} ${compactMoney.format(value)}`;
}

// Draws the value at the end of each horizontal bar, so the chart can be read
// without hovering
const barValueLabels: Plugin<'bar'> = {
  id: 'barValueLabels',
  afterDatasetsDraw(chart: Chart<'bar'>) {
    const { ctx } = chart;
    const meta = chart.getDatasetMeta(0);

    ctx.save();
    ctx.fillStyle = TEXT_MUTED;
    ctx.font = '500 12px system-ui, sans-serif';
    ctx.textBaseline = 'middle';

    meta.data.forEach((element, index) => {
      const value = chart.data.datasets[0].data[index];

      if (typeof value !== 'number') {
        return;
      }

      ctx.fillText(money(value), element.x + 8, element.y);
    });

    ctx.restore();
  },
};

// Money spent per month
export function monthlySpendChart(
  labels: string[],
  values: number[],
): ChartConfiguration<'bar'> {
  return {
    type: 'bar',
    data: {
      labels,
      datasets: [
        {
          label: 'Spent',
          data: values,
          backgroundColor: SERIES_BLUE,
          borderRadius: 4,
          borderSkipped: false,
          maxBarThickness: 44,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      // A single series names itself in the card title
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: { label: (item) => money(item.parsed.y ?? 0) },
        },
      },
      scales: {
        x: {
          grid: { display: false },
          border: { display: false },
          ticks: { color: TEXT_MUTED },
        },
        y: {
          beginAtZero: true,
          grid: { color: GRID_COLOR },
          border: { display: false },
          ticks: { color: TEXT_MUTED, callback: (value) => axisMoney(Number(value)) },
        },
      },
    },
  };
}

// Where the money went, biggest first and darkest
export function spendByProductChart(
  labels: string[],
  values: number[],
): ChartConfiguration<'bar'> {
  return {
    type: 'bar',
    data: {
      labels,
      datasets: [
        {
          label: 'Spent',
          data: values,
          backgroundColor: labels.map(
            (_, index) => SEQUENTIAL_BLUE[Math.min(index, SEQUENTIAL_BLUE.length - 1)],
          ),
          borderRadius: 4,
          borderSkipped: false,
          maxBarThickness: 28,
        },
      ],
    },
    options: {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      layout: { padding: { right: 96 } },
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: { label: (item) => money(item.parsed.x ?? 0) },
        },
      },
      scales: {
        x: {
          beginAtZero: true,
          grid: { color: GRID_COLOR },
          border: { display: false },
          ticks: { color: TEXT_MUTED, callback: (value) => axisMoney(Number(value)) },
        },
        y: {
          grid: { display: false },
          border: { display: false },
          ticks: { color: TEXT_MUTED },
        },
      },
    },
    plugins: [barValueLabels],
  };
}
