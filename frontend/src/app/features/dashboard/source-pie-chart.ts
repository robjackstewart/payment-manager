import { Component, input, AfterViewInit, OnChanges, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { AgCharts, ModuleRegistry, PieSeriesModule } from 'ag-charts-community';

ModuleRegistry.registerModules([PieSeriesModule]);

export interface PieSlice {
  label: string;
  amount: number;
}

@Component({
  selector: 'app-source-pie-chart',
  standalone: true,
  template: `<div #container class="pie-container"></div>`,
  styles: [`
    .pie-container {
      width: 100%;
      height: 280px;
    }
  `],
})
export class SourcePieChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  readonly slices = input<PieSlice[]>([]);
  readonly currency = input<string>('');

  @ViewChild('container') private readonly containerRef!: ElementRef<HTMLDivElement>;

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
      container: this.containerRef.nativeElement,
      data: this.slices(),
      theme: {
        baseTheme: dark ? 'ag-material-dark' as const : 'ag-material' as const,
        params: { fontFamily },
      },
      background: { fill: 'transparent' },
      series: [{ type: 'pie' as const, angleKey: 'amount', calloutLabelKey: 'label' }],
    };
  }
}
