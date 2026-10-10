import {
  afterNextRender,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

Chart.register(...registerables);

export interface LineSeries {
  label: string;
  data: number[];
  /** Emphasized series (e.g. totals) are drawn thicker and filled. */
  emphasis?: boolean;
}

const PALETTE = ['#1565c0', '#2e7d32', '#ef6c00', '#6a1b9a', '#c62828', '#00838f', '#ad1457', '#4e342e', '#283593', '#9e9d24'];

/** Thin, reactive wrapper around a Chart.js line chart. */
@Component({
  selector: 'app-line-chart',
  template: `<div class="chart-host" [style.height.px]="height()"><canvas #canvas></canvas></div>`,
  styles: `
    .chart-host { position: relative; width: 100%; }
  `,
})
export class LineChart {
  readonly labels = input.required<string[]>();
  readonly series = input.required<LineSeries[]>();
  readonly height = input(280);
  readonly yMax = input<number | undefined>(undefined);
  readonly yLabel = input<string>('');

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart?: Chart<'line'>;

  constructor() {
    afterNextRender(() => {
      this.chart = new Chart(this.canvas().nativeElement, this.buildConfig());
    });

    effect(() => {
      const config = this.buildConfig();
      if (this.chart) {
        this.chart.data = config.data;
        this.chart.options = config.options!;
        this.chart.update();
      }
    });

    inject(DestroyRef).onDestroy(() => this.chart?.destroy());
  }

  private buildConfig(): ChartConfiguration<'line'> {
    const series = this.series();
    return {
      type: 'line',
      data: {
        labels: this.labels(),
        datasets: series.map((s, i) => {
          const color = PALETTE[i % PALETTE.length];
          return {
            label: s.label,
            data: s.data,
            borderColor: color,
            backgroundColor: s.emphasis ? `${color}22` : color,
            borderWidth: s.emphasis ? 3 : 2,
            fill: !!s.emphasis,
            tension: 0.3,
            pointRadius: 2,
            pointHoverRadius: 5,
          };
        }),
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, usePointStyle: true } } },
        scales: {
          y: {
            beginAtZero: true,
            max: this.yMax(),
            title: { display: !!this.yLabel(), text: this.yLabel() },
            ticks: { precision: 0 },
          },
        },
      },
    };
  }
}
