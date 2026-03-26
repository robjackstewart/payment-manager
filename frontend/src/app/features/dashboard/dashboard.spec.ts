import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of } from 'rxjs';
import { AgCharts } from 'ag-charts-community';
import { ContactService } from '../../core/services/contact.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PaymentService } from '../../core/services/payment.service';
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

function setup(getOccurrencesMock?: ReturnType<typeof vi.fn>) {
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

  TestBed.configureTestingModule({
    imports: [DashboardComponent],
    providers: [
      { provide: PaymentService, useValue: mockPaymentService },
      { provide: PayeeService, useValue: mockPayeeService },
      { provide: PaymentSourceService, useValue: mockPaymentSourceService },
      { provide: ContactService, useValue: mockContactService },
      provideNativeDateAdapter(),
    ],
    schemas: [NO_ERRORS_SCHEMA],
  });

  const fixture = TestBed.createComponent(DashboardComponent);
  return { fixture, component: fixture.componentInstance, mockPaymentService };
}

describe('DashboardComponent', () => {
  beforeEach(() => TestBed.resetTestingModule());
  afterEach(() => vi.restoreAllMocks());

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

  describe('onMonthSelected()', () => {
    it('updates the private selectedMonth signal', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const newDate = new Date(2024, 5, 1);
      component.onMonthSelected(newDate, { close: vi.fn() } as any);

      expect((component as any).selectedMonth()).toEqual(newDate);
    });

    it('updates selectedMonthLabel to reflect the selected month and year', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      component.onMonthSelected(new Date(2024, 5, 1), { close: vi.fn() } as any);

      expect(component.selectedMonthLabel()).toContain('June');
      expect(component.selectedMonthLabel()).toContain('2024');
    });

    it('closes the picker', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const picker = { close: vi.fn() } as any;
      component.onMonthSelected(new Date(2024, 5, 1), picker);

      expect(picker.close).toHaveBeenCalled();
    });
  });
});
