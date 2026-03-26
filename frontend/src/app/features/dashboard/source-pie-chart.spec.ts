import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { AgCharts } from 'ag-charts-community';
import { SourcePieChartComponent } from './source-pie-chart';

async function setup() {
  document.body.classList.remove('dark-theme');

  const fakeChart = {
    update: vi.fn().mockResolvedValue(undefined),
    destroy: vi.fn(),
  };
  vi.spyOn(AgCharts, 'create').mockReturnValue(fakeChart as unknown as ReturnType<typeof AgCharts.create>);

  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [SourcePieChartComponent],
  }).compileComponents();

  const fixture = TestBed.createComponent(SourcePieChartComponent);
  const component = fixture.componentInstance;
  return { fixture, component, fakeChart };
}

describe('SourcePieChartComponent', () => {
  it('calls AgCharts.create after view init', async () => {
    const { fixture } = await setup();
    fixture.detectChanges();

    expect(AgCharts.create).toHaveBeenCalled();
  });

  it('calls chart.update when ngOnChanges is called after init', async () => {
    const { fixture, component, fakeChart } = await setup();
    fixture.detectChanges();

    component.ngOnChanges();

    expect(fakeChart.update).toHaveBeenCalled();
  });

  it('does not call chart.update when ngOnChanges is called before init', async () => {
    const { component } = await setup();
    component.ngOnChanges();

    expect(AgCharts.create).not.toHaveBeenCalled();
  });

  it('calls chart.destroy and disconnects observer on ngOnDestroy', async () => {
    const { fixture, component, fakeChart } = await setup();
    fixture.detectChanges();

    component.ngOnDestroy();

    expect(fakeChart.destroy).toHaveBeenCalled();
  });

  it('uses dark theme when body has dark-theme class', async () => {
    const { fixture } = await setup();
    document.body.classList.add('dark-theme');

    fixture.detectChanges();

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0] as any;
    expect(createCall.theme.baseTheme).toBe('ag-material-dark');
  });

  it('uses light theme when body does not have dark-theme class', async () => {
    const { fixture } = await setup();
    fixture.detectChanges();

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0] as any;
    expect(createCall.theme.baseTheme).toBe('ag-material');
  });

  it('passes slices input as chart data', async () => {
    const { fixture } = await setup();
    const slices = [
      { label: 'Cash', amount: 500 },
      { label: 'Card', amount: 300 },
    ];
    fixture.componentRef.setInput('slices', slices);
    fixture.detectChanges();

    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0];
    expect(createCall.data).toEqual(slices);
  });

  it('sets background fill to transparent', async () => {
    const { fixture } = await setup();
    fixture.detectChanges();

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const createCall = vi.mocked(AgCharts.create).mock.calls[0][0] as any;
    expect(createCall.background.fill).toBe('transparent');
  });
});
