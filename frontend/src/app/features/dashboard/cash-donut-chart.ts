import { Component, input, AfterViewInit, OnChanges, OnDestroy, ElementRef, viewChild } from '@angular/core';
import { AgCharts, ModuleRegistry, PieSeriesModule } from 'ag-charts-community';

ModuleRegistry.registerModules([PieSeriesModule]);

export interface DonutSlice {
  label: string;
  amount: number;
}

/**
 * A donut chart whose ring represents a payer group's total income for a month, with
 * one slice per payee it went to plus a "Left over" slice for what's still available.
 * Structurally a variant of PaymentsPieChartComponent with an inner radius — kept as a
 * separate component because its slices carry a different meaning (income breakdown,
 * not a cost total) and it will diverge further (e.g. a centre total label).
 */
@Component({
  selector: 'app-cash-donut-chart',
  template: `<div #container class="donut-container"></div>`,
  styles: [`
    .donut-container {
      width: 100%;
      height: 280px;
    }
  `],
})
export class CashDonutChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  readonly slices = input<DonutSlice[]>([]);
  readonly currency = input<string>('');

  private readonly containerRef = viewChild.required<ElementRef<HTMLDivElement>>('container');

  private chart?: ReturnType<typeof AgCharts.create>;
  private themeObserver?: MutationObserver;

  ngAfterViewInit(): void {
    this.chart = AgCharts.create(this.buildOptions());

    this.themeObserver = new MutationObserver(() => {
      if (this.chart) void this.chart.update(this.buildOptions());
    });
    this.themeObserver.observe(document.body, { attributes: true, attributeFilter: ['class'] });
  }

  ngOnChanges(): void {
    if (this.chart) {
      void this.chart.update(this.buildOptions());
    }
  }

  ngOnDestroy(): void {
    this.themeObserver?.disconnect();
    this.chart?.destroy();
  }

  private buildOptions() {
    const dark = document.body.classList.contains('dark-theme');
    const fontFamily = getComputedStyle(document.body).fontFamily;
    return {
      container: this.containerRef().nativeElement,
      data: this.slices(),
      theme: {
        baseTheme: dark ? 'ag-material-dark' as const : 'ag-material' as const,
        params: { fontFamily },
      },
      background: { fill: 'transparent' },
      series: [{ type: 'pie' as const, angleKey: 'amount', calloutLabelKey: 'label', innerRadiusRatio: 0.65 }],
    };
  }
}
