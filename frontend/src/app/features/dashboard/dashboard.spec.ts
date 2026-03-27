import { describe, expect, it, vi } from 'vitest';
import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepicker } from '@angular/material/datepicker';
import { of } from 'rxjs';
import { AgCharts } from 'ag-charts-community';
import { ContactService } from '../../core/services/contact.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PaymentService } from '../../core/services/payment.service';
import { BreakpointService } from '../../core/services/breakpoint.service';
import { DashboardComponent } from './dashboard';

const mockPayee = { id: 'py1', name: 'Alice' };
const mockPaymentSource = { id: 'ps1', name: 'Bank' };
const mockContact = { id: 'c1', name: 'Bob' };

const mockOccurrence = {
  id: 'o1', paymentSourceId: 'ps1', payeeId: 'py1',
  occurrenceDate: '2024-03-15', currency: 'USD',
  amount: 100, description: 'Rent',
  userShare: { percentage: 50, value: 50 },
};

const mockSummary = {
  currency: 'USD', totalAmount: 200, userTotal: 100,
  contactTotals: [{ contactId: 'c1', amount: 50 }],
  byPaymentSource: [{
    paymentSourceId: 'ps1', totalAmount: 200, userTotal: 100,
    contactTotals: [{ contactId: 'c1', amount: 50 }],
  }],
};

function makeOccurrenceResponse(
  summary = mockSummary,
  occurrences = [mockOccurrence],
) {
  return { occurrences, summary: [summary] };
}

function setup(getOccurrencesMock?: ReturnType<typeof vi.fn>, isMobile = false) {
  TestBed.resetTestingModule();
  vi.spyOn(AgCharts, 'create').mockReturnValue({
    update: vi.fn().mockResolvedValue(undefined),
    destroy: vi.fn(),
  } as unknown as ReturnType<typeof AgCharts.create>);

  const mockPaymentService = {
    getOccurrences: getOccurrencesMock ?? vi.fn().mockReturnValue(of(makeOccurrenceResponse())),
  };
  const mockPayeeService = { getAll: vi.fn().mockReturnValue(of([mockPayee])) };
  const mockPaymentSourceService = { getAll: vi.fn().mockReturnValue(of([mockPaymentSource])) };
  const mockContactService = { getAll: vi.fn().mockReturnValue(of([mockContact])) };
  const isMobileSignal = signal(isMobile);

  TestBed.configureTestingModule({
    imports: [DashboardComponent],
    providers: [
      { provide: PaymentService, useValue: mockPaymentService },
      { provide: PayeeService, useValue: mockPayeeService },
      { provide: PaymentSourceService, useValue: mockPaymentSourceService },
      { provide: ContactService, useValue: mockContactService },
      { provide: BreakpointService, useValue: { isMobile: isMobileSignal } },
      provideNativeDateAdapter(),
    ],
    schemas: [NO_ERRORS_SCHEMA],
  });

  const fixture = TestBed.createComponent(DashboardComponent);
  return { fixture, component: fixture.componentInstance, mockPaymentService, isMobileSignal };
}

describe('DashboardComponent', () => {
  describe('reference data', () => {
    it('exposes payees after loading', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.payees()).toEqual([mockPayee]);
    });

    it('exposes paymentSources after loading', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.paymentSources()).toEqual([mockPaymentSource]);
    });

    it('exposes contacts after loading', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.contacts()).toEqual([mockContact]);
    });
  });

  describe('occurrencesViewModel', () => {
    async function resolvedViewModel() {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();
      return component.occurrencesViewModel();
    }

    it('maps occurrenceDate to a formatted date string', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].formattedDate).toMatch(/Mar/);
      expect(vm[0].formattedDate).toMatch(/2024/);
    });

    it('resolves sourceName via paymentSourcesMap', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].sourceName).toBe('Bank');
    });

    it('resolves payeeName via payeesMap', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].payeeName).toBe('Alice');
    });

    it('uses description as descriptionDisplay when present', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].descriptionDisplay).toBe('Rent');
    });

    it('uses em dash as descriptionDisplay when description is falsy', async () => {
      const mock = vi.fn().mockReturnValue(
        of(makeOccurrenceResponse(mockSummary, [{ ...mockOccurrence, description: '' }])),
      );
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.occurrencesViewModel()[0].descriptionDisplay).toBe('—');
    });

    it('formats amount as a currency string', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].formattedAmount).toMatch(/\$100/);
    });

    it('renders integer percentage without decimal places', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].yourShareDisplay).toBe('50%');
    });

    it('renders fractional percentage with two decimal places', async () => {
      const mock = vi.fn().mockReturnValue(
        of(makeOccurrenceResponse(mockSummary, [
          { ...mockOccurrence, userShare: { percentage: 33.33, value: 33.33 } },
        ])),
      );
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.occurrencesViewModel()[0].yourShareDisplay).toBe('33.33%');
    });
  });

  describe('summaryViewModel — delta calculation', () => {
    it('sets deltaState to "same" when current and previous totals are equal', async () => {
      // forkJoin calls getOccurrences twice: first for current, second for previous
      const mock = vi.fn()
        .mockReturnValueOnce(of(makeOccurrenceResponse({ ...mockSummary, totalAmount: 200 })))
        .mockReturnValueOnce(of(makeOccurrenceResponse({ ...mockSummary, totalAmount: 200 })));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const vm = component.summaryViewModel();
      expect(vm[0].deltaState).toBe('same');
      expect(vm[0].delta).toBe('— Same as last month');
    });

    it('sets deltaState to "increase" and prefixes delta with ▲ when current > previous', async () => {
      const mock = vi.fn()
        .mockReturnValueOnce(of(makeOccurrenceResponse({ ...mockSummary, totalAmount: 300 })))
        .mockReturnValueOnce(of(makeOccurrenceResponse({ ...mockSummary, totalAmount: 200 })));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const vm = component.summaryViewModel();
      expect(vm[0].deltaState).toBe('increase');
      expect(vm[0].delta).toMatch(/^▲/);
    });

    it('sets deltaState to "decrease" and prefixes delta with ▼ when current < previous', async () => {
      const mock = vi.fn()
        .mockReturnValueOnce(of(makeOccurrenceResponse({ ...mockSummary, totalAmount: 100 })))
        .mockReturnValueOnce(of(makeOccurrenceResponse({ ...mockSummary, totalAmount: 200 })));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const vm = component.summaryViewModel();
      expect(vm[0].deltaState).toBe('decrease');
      expect(vm[0].delta).toMatch(/^▼/);
    });

    it('sets delta and deltaState to null when there is no previous month summary', async () => {
      const mock = vi.fn()
        .mockReturnValueOnce(of(makeOccurrenceResponse()))
        .mockReturnValueOnce(of({ occurrences: [], summary: [] }));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const vm = component.summaryViewModel();
      expect(vm[0].delta).toBeNull();
      expect(vm[0].deltaState).toBeNull();
    });
  });

  describe('summaryViewModel — structure', () => {
    it('builds contacts array with resolved name and formatted amount', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const { contacts } = component.summaryViewModel()[0];
      expect(contacts).toHaveLength(1);
      expect(contacts[0].name).toBe('Bob');
      expect(contacts[0].amount).toMatch(/\$50/);
    });

    it('builds byPaymentSource with resolved source name and formatted amounts', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const [source] = component.summaryViewModel()[0].byPaymentSource;
      expect(source.sourceName).toBe('Bank');
      expect(source.totalAmount).toMatch(/\$200/);
      expect(source.userTotal).toMatch(/\$100/);
    });

    it('builds byPaymentSource contacts with resolved name', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const { contacts } = component.summaryViewModel()[0].byPaymentSource[0];
      expect(contacts[0].name).toBe('Bob');
      expect(contacts[0].amount).toMatch(/\$50/);
    });

    it('builds pieSlices with correct label and raw amount', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const [slice] = component.summaryViewModel()[0].pieSlices;
      expect(slice.label).toBe('Bank');
      expect(slice.amount).toBe(200);
    });
  });

  describe('schedulePayeeSlices', () => {
    it('returns empty array when there are no occurrences', async () => {
      const mock = vi.fn().mockReturnValue(of({ occurrences: [], summary: [] }));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.schedulePayeeSlices()).toEqual([]);
    });

    it('returns one group with a single slice for a single payee', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const groups = component.schedulePayeeSlices();
      expect(groups).toHaveLength(1);
      expect(groups[0].currency).toBe('USD');
      expect(groups[0].slices).toEqual([{ label: 'Alice', amount: 100 }]);
    });

    it('sums amounts for the same payee across multiple occurrences', async () => {
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse(mockSummary, [
        { ...mockOccurrence, id: 'o1', amount: 60 },
        { ...mockOccurrence, id: 'o2', amount: 40 },
      ])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const [group] = component.schedulePayeeSlices();
      expect(group.slices).toEqual([{ label: 'Alice', amount: 100 }]);
    });

    it('returns separate slices for different payees in the same currency', async () => {
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse(mockSummary, [
        { ...mockOccurrence, id: 'o1', payeeId: 'py1', amount: 100 },
        { ...mockOccurrence, id: 'o2', payeeId: 'py2', amount: 200 },
      ])));
      const mockPayeeService2 = { getAll: vi.fn().mockReturnValue(of([mockPayee, { id: 'py2', name: 'Carol' }])) };
      TestBed.resetTestingModule();
      vi.spyOn(AgCharts, 'create').mockReturnValue({ update: vi.fn().mockResolvedValue(undefined), destroy: vi.fn() } as unknown as ReturnType<typeof AgCharts.create>);
      TestBed.configureTestingModule({
        imports: [DashboardComponent],
        providers: [
          { provide: PaymentService, useValue: { getOccurrences: mock } },
          { provide: PayeeService, useValue: mockPayeeService2 },
          { provide: PaymentSourceService, useValue: { getAll: vi.fn().mockReturnValue(of([mockPaymentSource])) } },
          { provide: ContactService, useValue: { getAll: vi.fn().mockReturnValue(of([mockContact])) } },
          { provide: BreakpointService, useValue: { isMobile: signal(false) } },
          provideNativeDateAdapter(),
        ],
        schemas: [NO_ERRORS_SCHEMA],
      });
      const fixture = TestBed.createComponent(DashboardComponent);
      fixture.detectChanges();
      await fixture.whenStable();

      const [group] = fixture.componentInstance.schedulePayeeSlices();
      expect(group.slices).toContainEqual({ label: 'Alice', amount: 100 });
      expect(group.slices).toContainEqual({ label: 'Carol', amount: 200 });
    });

    it('returns separate groups for occurrences in different currencies', async () => {
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse(mockSummary, [
        { ...mockOccurrence, id: 'o1', currency: 'USD', amount: 100 },
        { ...mockOccurrence, id: 'o2', currency: 'EUR', amount: 50 },
      ])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const groups = component.schedulePayeeSlices();
      expect(groups).toHaveLength(2);
      expect(groups.map(g => g.currency)).toContain('USD');
      expect(groups.map(g => g.currency)).toContain('EUR');
    });

    it('falls back to payeeId when payee is not in the map', async () => {
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse(mockSummary, [
        { ...mockOccurrence, payeeId: 'unknown-py' },
      ])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const [group] = component.schedulePayeeSlices();
      expect(group.slices[0].label).toBe('unknown-py');
    });
  });

  describe('onMonthSelected()', () => {
    it('updates the private selectedMonth signal', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const newDate = new Date(2024, 5, 1);
      component.onMonthSelected(newDate, { close: vi.fn() } as unknown as MatDatepicker<Date>);

      expect((component as unknown as { selectedMonth: () => Date }).selectedMonth()).toEqual(newDate);
    });

    it('updates selectedMonthLabel to reflect the selected month and year', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      component.onMonthSelected(new Date(2024, 5, 1), { close: vi.fn() } as unknown as MatDatepicker<Date>);

      expect(component.selectedMonthLabel()).toContain('June');
      expect(component.selectedMonthLabel()).toContain('2024');
    });

    it('closes the picker', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const picker = { close: vi.fn() } as unknown as MatDatepicker<Date>;
      component.onMonthSelected(new Date(2024, 5, 1), picker);

      expect(picker.close).toHaveBeenCalled();
    });
  });

  describe('responsive rendering — payment schedule', () => {
    it('on desktop renders the schedule table and no mobile card list', async () => {
      const { fixture } = setup(undefined, false);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).not.toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).toBeNull();
    });

    it('on mobile renders the card list and no schedule table', async () => {
      const { fixture } = setup(undefined, true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).not.toBeNull();
    });

    it('on mobile each occurrence card shows the payee name as the card title', async () => {
      const { fixture } = setup(undefined, true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const titles = Array.from(
        fixture.nativeElement.querySelectorAll('.card-title') as NodeListOf<HTMLElement>,
      ).map(el => el.textContent?.trim());
      expect(titles).toContain('Alice');
    });

    it('on mobile the date appears as a card-field, not the card title', async () => {
      const { fixture } = setup(undefined, true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const dateField = Array.from(
        fixture.nativeElement.querySelectorAll('.card-field .card-label') as NodeListOf<HTMLElement>,
      ).find(el => el.textContent?.trim() === 'Date');
      expect(dateField).not.toBeUndefined();
    });
  });
});
