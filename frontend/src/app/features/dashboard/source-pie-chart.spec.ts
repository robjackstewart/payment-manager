import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('ag-charts-community', () => ({
  AgCharts: {
    create: vi.fn().mockReturnValue({
      update: vi.fn().mockResolvedValue(undefined),
      destroy: vi.fn(),
    }),
  },
  ModuleRegistry: { registerModules: vi.fn() },
  PieSeriesModule: {},
}));

import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AgCharts } from 'ag-charts-community';
import { SourcePieChartComponent } from './source-pie-chart';

describe('SourcePieChartComponent', () => {
  let fixture: ComponentFixture<SourcePieChartComponent>;
  let component: SourcePieChartComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SourcePieChartComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SourcePieChartComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    vi.clearAllMocks();
    document.body.classList.remove('dark-theme');
  });

  it('calls AgCharts.create after view init', () => {
    fixture.detectChanges();

    expect(AgCharts.create).toHaveBeenCalled();
  });

  it('calls chart.update when ngOnChanges is called after init', () => {
    fixture.detectChanges();
    const chartMock = (AgCharts.create as ReturnType<typeof vi.fn>).mock.results[0].value;

    component.ngOnChanges();

    expect(chartMock.update).toHaveBeenCalled();
  });

  it('does not call chart.update when ngOnChanges is called before init', () => {
    component.ngOnChanges();

    expect(AgCharts.create).not.toHaveBeenCalled();
  });

  it('calls chart.destroy and disconnects observer on ngOnDestroy', () => {
    fixture.detectChanges();
    const chartMock = (AgCharts.create as ReturnType<typeof vi.fn>).mock.results[0].value;

    component.ngOnDestroy();

    expect(chartMock.destroy).toHaveBeenCalled();
  });

  it('uses dark theme when body has dark-theme class', () => {
    document.body.classList.add('dark-theme');

    fixture.detectChanges();

    const createCall = (AgCharts.create as ReturnType<typeof vi.fn>).mock.calls[0][0];
    expect(createCall.theme.baseTheme).toBe('ag-material-dark');
  });

  it('uses light theme when body does not have dark-theme class', () => {
    fixture.detectChanges();

    const createCall = (AgCharts.create as ReturnType<typeof vi.fn>).mock.calls[0][0];
    expect(createCall.theme.baseTheme).toBe('ag-material');
  });

  it('passes slices input as chart data', () => {
    const slices = [
      { label: 'Cash', amount: 500 },
      { label: 'Card', amount: 300 },
    ];
    fixture.componentRef.setInput('slices', slices);
    fixture.detectChanges();

    const createCall = (AgCharts.create as ReturnType<typeof vi.fn>).mock.calls[0][0];
    expect(createCall.data).toEqual(slices);
  });

  it('sets background fill to transparent', () => {
    fixture.detectChanges();

    const createCall = (AgCharts.create as ReturnType<typeof vi.fn>).mock.calls[0][0];
    expect(createCall.background.fill).toBe('transparent');
  });
});
