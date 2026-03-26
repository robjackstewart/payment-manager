import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { AgCharts } from 'ag-charts-community';
import { SourcePieChartComponent } from './source-pie-chart';

describe('SourcePieChartComponent', () => {
  let fixture: ComponentFixture<SourcePieChartComponent>;
  let component: SourcePieChartComponent;
  let fakeChart: { update: ReturnType<typeof vi.fn>; destroy: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    fakeChart = {
      update: vi.fn().mockResolvedValue(undefined),
      destroy: vi.fn(),
    };
    vi.spyOn(AgCharts, 'create').mockReturnValue(fakeChart as unknown as ReturnType<typeof AgCharts.create>);

    await TestBed.configureTestingModule({
      imports: [SourcePieChartComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SourcePieChartComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    vi.restoreAllMocks();
    document.body.classList.remove('dark-theme');
  });

  it('calls AgCharts.create after view init', () => {
    fixture.detectChanges();

    expect(AgCharts.create).toHaveBeenCalled();
  });

  it('calls chart.update when ngOnChanges is called after init', () => {
    fixture.detectChanges();

    component.ngOnChanges();

    expect(fakeChart.update).toHaveBeenCalled();
  });

  it('does not call chart.update when ngOnChanges is called before init', () => {
    component.ngOnChanges();

    expect(AgCharts.create).not.toHaveBeenCalled();
  });

  it('calls chart.destroy and disconnects observer on ngOnDestroy', () => {
    fixture.detectChanges();

    component.ngOnDestroy();

    expect(fakeChart.destroy).toHaveBeenCalled();
  });

  it('uses dark theme when body has dark-theme class', () => {
    document.body.classList.add('dark-theme');

    fixture.detectChanges();

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0] as any;
    expect(createCall.theme.baseTheme).toBe('ag-material-dark');
  });

  it('uses light theme when body does not have dark-theme class', () => {
    fixture.detectChanges();

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0] as any;
    expect(createCall.theme.baseTheme).toBe('ag-material');
  });

  it('passes slices input as chart data', () => {
    const slices = [
      { label: 'Cash', amount: 500 },
      { label: 'Card', amount: 300 },
    ];
    fixture.componentRef.setInput('slices', slices);
    fixture.detectChanges();

    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0];
    expect(createCall.data).toEqual(slices);
  });

  it('sets background fill to transparent', () => {
    fixture.detectChanges();

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0] as any;
    expect(createCall.background.fill).toBe('transparent');
  });
});
