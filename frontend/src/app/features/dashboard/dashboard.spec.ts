import { describe, expect, it, vi } from 'vitest';
import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepicker } from '@angular/material/datepicker';
import { of } from 'rxjs';
import { AgCharts } from 'ag-charts-community';
import { PersonService } from '../../core/services/person.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PaymentService } from '../../core/services/payment.service';
import { PayerGroupService } from '../../core/services/payer-group.service';
import { BreakpointService } from '../../core/services/breakpoint.service';
import { PaymentDirection } from '../../core/models/payment-direction.enum';
import { DashboardComponent } from './dashboard';

const mockPayee = { id: 'py1', name: 'Alice' };
const mockPaymentSource = { id: 'ps1', name: 'Bank' };
const mockCurrentUser = { id: 'self', userId: 'u1', name: 'Current User' };
const mockBob = { id: 'c1', userId: 'u1', name: 'Bob' };
const mockPeople = [mockCurrentUser, mockBob];
const mockPayerGroup = { id: 'g1', userId: 'u1', name: 'Family', memberPersonIds: ['self', 'c1'] };

const mockOccurrence = {
  paymentId: 'o1', paymentSourceId: 'ps1', payeeId: 'py1',
  occurrenceDate: '2024-03-15', currency: 'USD',
  amount: 100, direction: PaymentDirection.Outgoing, payerGroupId: null as string | null,
  description: 'Rent',
  splits: [
    { personId: 'self', percentage: 50 },
    { personId: 'c1', percentage: 50 },
  ],
};

function makePersonAmounts(amounts: Record<string, number>) {
  return Object.entries(amounts).map(([personId, amount]) => ({ personId, amount }));
}

function makeDirectionTotals(totalAmount: number, personAmounts: Record<string, number> = {}) {
  return { totalAmount, personTotals: makePersonAmounts(personAmounts), byPaymentSource: [] };
}

function makeNetTotals(totalAmount: number, personAmounts: Record<string, number> = {}) {
  return { totalAmount, personTotals: makePersonAmounts(personAmounts) };
}

const mockCurrencySummary = {
  currency: 'USD',
  outgoing: makeDirectionTotals(200, { self: 100, c1: 100 }),
  incoming: makeDirectionTotals(0, {}),
  net: makeNetTotals(-200, { self: -100, c1: -100 }),
  outgoingByPayee: [{ payeeId: 'py1', amount: 200 }],
};

const mockGroupSummary = { payerGroupId: 'g1', currencies: [mockCurrencySummary] };

interface MockCommitment {
  personId: string;
  currency: string;
  income: number;
  committed: number;
  remaining: number;
  isOverCommitted: boolean;
}

function makeOccurrenceResponse(
  summary = [mockGroupSummary],
  occurrences = [mockOccurrence],
  people: MockCommitment[] = [],
) {
  return { occurrences, summary, people };
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
  const mockPersonService = { getAll: vi.fn().mockReturnValue(of(mockPeople)) };
  const mockPayerGroupService = { getAll: vi.fn().mockReturnValue(of([mockPayerGroup])) };
  const isMobileSignal = signal(isMobile);

  TestBed.configureTestingModule({
    imports: [DashboardComponent],
    providers: [
      { provide: PaymentService, useValue: mockPaymentService },
      { provide: PayeeService, useValue: mockPayeeService },
      { provide: PaymentSourceService, useValue: mockPaymentSourceService },
      { provide: PersonService, useValue: mockPersonService },
      { provide: PayerGroupService, useValue: mockPayerGroupService },
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

    it('exposes people after loading', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.people()).toEqual(mockPeople);
    });

    it('exposes payerGroups after loading', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.payerGroups()).toEqual([mockPayerGroup]);
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
        of(makeOccurrenceResponse([mockGroupSummary], [{ ...mockOccurrence, description: '' }])),
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

    it('renders every split with its person name and percentage', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].splitDisplay).toBe('Current User 50% · Bob 50%');
    });

    it('renders fractional split percentages with two decimal places', async () => {
      const mock = vi.fn().mockReturnValue(
        of(makeOccurrenceResponse([mockGroupSummary], [
          {
            ...mockOccurrence,
            splits: [
              { personId: 'self', percentage: 33.33 },
              { personId: 'c1', percentage: 66.67 },
            ],
          },
        ])),
      );
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.occurrencesViewModel()[0].splitDisplay).toBe('Current User 33.33% · Bob 66.67%');
    });

    it('marks an outgoing occurrence with the amount-out class and a minus sign', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].directionClass).toBe('amount-out');
      expect(vm[0].signedAmountDisplay).toMatch(/^−/);
    });

    it('marks an incoming occurrence with the amount-in class and a plus sign', async () => {
      const mock = vi.fn().mockReturnValue(
        of(makeOccurrenceResponse([mockGroupSummary], [
          { ...mockOccurrence, direction: PaymentDirection.Incoming },
        ])),
      );
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const vm = component.occurrencesViewModel();
      expect(vm[0].directionClass).toBe('amount-in');
      expect(vm[0].signedAmountDisplay).toMatch(/^\+/);
    });
  });

  describe('groupCardViewModels', () => {
    it('resolves the payer group name for the summary entry', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.groupCardViewModels()[0].title).toBe('Family — USD');
    });

    it('falls back to the payer group id when the group name is unknown', async () => {
      const unknown = { payerGroupId: 'g-unknown', currencies: [mockCurrencySummary] };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([unknown])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.groupCardViewModels()[0].title).toBe('g-unknown — USD');
    });

    it('shows income and outgoing tiles formatted as currency', async () => {
      const summary = {
        ...mockGroupSummary,
        currencies: [{
          ...mockCurrencySummary,
          incoming: makeDirectionTotals(500, { self: 500 }),
          outgoing: makeDirectionTotals(200, { self: 100, c1: 100 }),
        }],
      };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([summary])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const card = component.groupCardViewModels()[0];
      expect(card.incomeDisplay).toMatch(/\$500/);
      expect(card.outgoingDisplay).toMatch(/\$200/);
    });

    it('shows "Left Over" when income exceeds outgoings', async () => {
      const summary = {
        ...mockGroupSummary,
        currencies: [{ ...mockCurrencySummary, incoming: makeDirectionTotals(500, { self: 500 }), outgoing: makeDirectionTotals(200, { self: 100 }) }],
      };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([summary])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const card = component.groupCardViewModels()[0];
      expect(card.isOverspent).toBe(false);
      expect(card.thirdTileLabel).toBe('Left Over');
      expect(card.thirdTileValue).toMatch(/\$300/);
    });

    it('shows "Overspent" when outgoings exceed income', async () => {
      const summary = {
        ...mockGroupSummary,
        currencies: [{ ...mockCurrencySummary, incoming: makeDirectionTotals(100, { self: 100 }), outgoing: makeDirectionTotals(300, { self: 300 }) }],
      };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([summary])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const card = component.groupCardViewModels()[0];
      expect(card.isOverspent).toBe(true);
      expect(card.thirdTileLabel).toBe('Overspent');
      expect(card.thirdTileValue).toMatch(/\$200/);
    });

    it('builds donut slices from outgoingByPayee plus a Left Over slice when income exceeds outgoings', async () => {
      const summary = {
        ...mockGroupSummary,
        currencies: [{
          ...mockCurrencySummary,
          incoming: makeDirectionTotals(500, { self: 500 }),
          outgoing: makeDirectionTotals(200, { self: 100 }),
          outgoingByPayee: [{ payeeId: 'py1', amount: 200 }],
        }],
      };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([summary])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const card = component.groupCardViewModels()[0];
      expect(card.showDonut).toBe(true);
      expect(card.donutSlices).toContainEqual({ label: 'Alice', amount: 200 });
      expect(card.donutSlices).toContainEqual({ label: 'Left over', amount: 300 });
    });

    it('does not show a donut when the group is overspent', async () => {
      const summary = {
        ...mockGroupSummary,
        currencies: [{ ...mockCurrencySummary, incoming: makeDirectionTotals(100, { self: 100 }), outgoing: makeDirectionTotals(300, { self: 300 }), outgoingByPayee: [{ payeeId: 'py1', amount: 300 }] }],
      };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([summary])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.groupCardViewModels()[0].showDonut).toBe(false);
    });

    it('does not show a donut when there is no income at all', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      // mockCurrencySummary has incoming.totalAmount = 0
      expect(component.groupCardViewModels()[0].showDonut).toBe(false);
    });

    it('builds a person row for every participant, with no special-cased user', async () => {
      const summary = {
        ...mockGroupSummary,
        currencies: [
          {
            ...mockCurrencySummary,
            incoming: makeDirectionTotals(0, {}),
            outgoing: makeDirectionTotals(200, { self: 100, c1: 100 }),
            net: makeNetTotals(-200, { self: -100, c1: -100 }),
          },
        ],
      };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([summary])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const card = component.groupCardViewModels()[0];
      expect(card.people).toHaveLength(2);
      expect(card.people.map(p => p.name)).toEqual(['Current User', 'Bob']);
      expect(card.people.every(p => p.netState === 'negative')).toBe(true);
    });

    it('resolves person names and net state on group rows', async () => {
      const summary = {
        ...mockGroupSummary,
        currencies: [{
          ...mockCurrencySummary,
          incoming: makeDirectionTotals(0, {}),
          outgoing: makeDirectionTotals(200, { self: 100, c1: 100 }),
          net: makeNetTotals(-100, { c1: -100 }),
        }],
      };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([summary])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const bob = component.groupCardViewModels()[0].people.find(p => p.personId === 'c1')!;
      expect(bob.name).toBe('Bob');
      expect(bob.netState).toBe('negative');
    });

    it('flags a person as over-committed on the group row and in the global People section', async () => {
      const people: MockCommitment[] = [
        { personId: 'c1', currency: 'USD', income: 100, committed: 250, remaining: -150, isOverCommitted: true },
      ];
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([mockGroupSummary], [mockOccurrence], people)));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const bob = component.groupCardViewModels()[0].people.find(p => p.personId === 'c1')!;
      expect(bob.isOverCommitted).toBe(true);

      const row = component.commitmentRows()[0];
      expect(row.name).toBe('Bob');
      expect(row.isOverCommitted).toBe(true);
      expect(row.remainingDisplay).toMatch(/-\$150/);
    });

    it('computes a month-over-month delta on outgoing totals', async () => {
      const current = { ...mockGroupSummary, currencies: [{ ...mockCurrencySummary, outgoing: makeDirectionTotals(300, { self: 300 }) }] };
      const previous = { ...mockGroupSummary, currencies: [{ ...mockCurrencySummary, outgoing: makeDirectionTotals(200, { self: 200 }) }] };
      const mock = vi.fn()
        .mockReturnValueOnce(of(makeOccurrenceResponse([current])))
        .mockReturnValueOnce(of(makeOccurrenceResponse([previous])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const card = component.groupCardViewModels()[0];
      expect(card.deltaState).toBe('increase');
      expect(card.delta).toMatch(/^▲/);
    });

    it('leaves delta null when there is no matching group in the previous month', async () => {
      const mock = vi.fn()
        .mockReturnValueOnce(of(makeOccurrenceResponse()))
        .mockReturnValueOnce(of({ occurrences: [], summary: [], people: [] }));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(component.groupCardViewModels()[0].delta).toBeNull();
      expect(component.groupCardViewModels()[0].deltaState).toBeNull();
    });

    it('renders separate cards for each payer group in the same month', async () => {
      const family = { payerGroupId: 'g1', currencies: [mockCurrencySummary] };
      const work = { payerGroupId: 'g2', currencies: [mockCurrencySummary] };
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([family, work])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const titles = component.groupCardViewModels().map(c => c.title);
      expect(titles).toContain('Family — USD');
      expect(titles).toContain('g2 — USD');
    });
  });

  describe('costBreakdownSlices', () => {
    it('aggregates outgoing occurrences by payment source', async () => {
      const { fixture, component } = setup();
      fixture.detectChanges();
      await fixture.whenStable();

      const groups = component.costBreakdownSlices();
      expect(groups).toHaveLength(1);
      expect(groups[0].currency).toBe('USD');
      expect(groups[0].slices).toEqual([{ label: 'Bank', amount: 100 }]);
    });

    it('excludes incoming occurrences', async () => {
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([mockGroupSummary], [
        { ...mockOccurrence, paymentId: 'o1', direction: PaymentDirection.Outgoing, amount: 100 },
        { ...mockOccurrence, paymentId: 'o2', direction: PaymentDirection.Incoming, amount: 3000 },
      ])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const [group] = component.costBreakdownSlices();
      expect(group.slices).toEqual([{ label: 'Bank', amount: 100 }]);
    });
  });

  describe('schedulePayeeSlices', () => {
    it('returns empty array when there are no occurrences', async () => {
      const mock = vi.fn().mockReturnValue(of({ occurrences: [], summary: [], people: [] }));
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
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([mockGroupSummary], [
        { ...mockOccurrence, paymentId: 'o1', amount: 60 },
        { ...mockOccurrence, paymentId: 'o2', amount: 40 },
      ])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const [group] = component.schedulePayeeSlices();
      expect(group.slices).toEqual([{ label: 'Alice', amount: 100 }]);
    });

    it('excludes incoming occurrences from the schedule pie', async () => {
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([mockGroupSummary], [
        { ...mockOccurrence, paymentId: 'o1', payeeId: 'py1', amount: 100, direction: PaymentDirection.Outgoing },
        { ...mockOccurrence, paymentId: 'o2', payeeId: 'py1', amount: 5000, direction: PaymentDirection.Incoming },
      ])));
      const { fixture, component } = setup(mock);
      fixture.detectChanges();
      await fixture.whenStable();

      const [group] = component.schedulePayeeSlices();
      expect(group.slices).toEqual([{ label: 'Alice', amount: 100 }]);
    });

    it('returns separate slices for different payees in the same currency', async () => {
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([mockGroupSummary], [
        { ...mockOccurrence, paymentId: 'o1', payeeId: 'py1', amount: 100 },
        { ...mockOccurrence, paymentId: 'o2', payeeId: 'py2', amount: 200 },
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
          { provide: PersonService, useValue: { getAll: vi.fn().mockReturnValue(of(mockPeople)) } },
          { provide: PayerGroupService, useValue: { getAll: vi.fn().mockReturnValue(of([mockPayerGroup])) } },
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
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([mockGroupSummary], [
        { ...mockOccurrence, paymentId: 'o1', currency: 'USD', amount: 100 },
        { ...mockOccurrence, paymentId: 'o2', currency: 'EUR', amount: 50 },
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
      const mock = vi.fn().mockReturnValue(of(makeOccurrenceResponse([mockGroupSummary], [
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
