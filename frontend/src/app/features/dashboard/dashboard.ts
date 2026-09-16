import { Component, inject, signal, computed } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MAT_DATE_FORMATS, provideNativeDateAdapter } from '@angular/material/core';
import { MatCard, MatCardHeader, MatCardAvatar, MatCardTitle, MatCardSubtitle, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';
import { MatDivider } from '@angular/material/divider';
import { MatTooltip } from '@angular/material/tooltip';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentService } from '../../core/services/payment.service';
import { PersonService } from '../../core/services/person.service';
import { PayerGroupService } from '../../core/services/payer-group.service';
import { BreakpointService } from '../../core/services/breakpoint.service';
import { PaymentDirection } from '../../core/models/payment-direction.enum';
import { forkJoin } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { PaymentsPieChartComponent, PieSlice } from './payments-pie-chart';
import { CashDonutChartComponent, DonutSlice } from './cash-donut-chart';

interface OccurrenceViewModel {
  formattedDate: string;
  sourceName: string;
  payeeName: string;
  descriptionDisplay: string;
  formattedAmount: string;
  signedAmountDisplay: string;
  directionClass: 'amount-in' | 'amount-out';
  currency: string;
  splitDisplay: string;
}

interface PersonRow {
  personId: string;
  name: string;
  incomeDisplay: string;
  outgoingDisplay: string;
  netDisplay: string;
  netState: 'positive' | 'negative' | 'zero';
  isOverCommitted: boolean;
}

/** One row of the global People section: a person's income, committed share and headroom. */
interface CommitmentRow {
  personId: string;
  name: string;
  currency: string;
  incomeDisplay: string;
  committedDisplay: string;
  remainingDisplay: string;
  isOverCommitted: boolean;
}

interface PayeeSliceGroup {
  currency: string;
  slices: PieSlice[];
}

interface GroupCardViewModel {
  key: string;
  title: string;
  currency: string;
  incomeDisplay: string;
  outgoingDisplay: string;
  thirdTileLabel: string;
  thirdTileValue: string;
  isOverspent: boolean;
  delta: string | null;
  deltaState: 'increase' | 'decrease' | 'same' | null;
  donutSlices: DonutSlice[];
  showDonut: boolean;
  people: PersonRow[];
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  providers: [
    CurrencyPipe,
    DatePipe,
    provideNativeDateAdapter(),
    {
      provide: MAT_DATE_FORMATS,
      useValue: {
        parse: { dateInput: { month: 'long', year: 'numeric' } },
        display: {
          dateInput: { month: 'long', year: 'numeric' },
          monthYearLabel: { month: 'short', year: 'numeric' },
          dateA11yLabel: { year: 'numeric', month: 'long', day: 'numeric' },
          monthYearA11yLabel: { year: 'numeric', month: 'long' },
        },
      },
    },
  ],
  imports: [
    MatCard,
    MatCardHeader,
    MatCardAvatar,
    MatCardTitle,
    MatCardSubtitle,
    MatCardContent,
    MatIcon,
    MatProgressSpinner,
    MatTable,
    MatColumnDef,
    MatHeaderCell,
    MatHeaderCellDef,
    MatCell,
    MatCellDef,
    MatHeaderRow,
    MatHeaderRowDef,
    MatRow,
    MatRowDef,
    MatFormField,
    MatLabel,
    MatSuffix,
    MatInput,
    MatDatepicker,
    MatDatepickerInput,
    MatDatepickerToggle,
    MatDivider,
    MatTooltip,
    ReactiveFormsModule,
    PaymentsPieChartComponent,
    CashDonutChartComponent,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly paymentService = inject(PaymentService);
  private readonly personService = inject(PersonService);
  private readonly payerGroupService = inject(PayerGroupService);
  private readonly currencyPipe = inject(CurrencyPipe);
  private readonly datePipe = inject(DatePipe);
  readonly breakpointService = inject(BreakpointService);

  private readonly refDataResource = rxResource({
    stream: () => forkJoin({
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
      people: this.personService.getAll(),
      payerGroups: this.payerGroupService.getAll(),
    })
  });

  readonly loading = computed(() => this.refDataResource.isLoading());

  readonly payees = computed(() => this.refDataResource.value()?.payees ?? []);
  readonly paymentSources = computed(() => this.refDataResource.value()?.paymentSources ?? []);
  readonly people = computed(() => this.refDataResource.value()?.people ?? []);
  readonly payerGroups = computed(() => this.refDataResource.value()?.payerGroups ?? []);

  private readonly payeesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.payees()) map[p.id] = p.name;
    return map;
  });

  private readonly paymentSourcesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const ps of this.paymentSources()) map[ps.id] = ps.name;
    return map;
  });

  private readonly peopleMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.people()) map[p.id] = p.name;
    return map;
  });

  private readonly payerGroupsMap = computed(() => {
    const map: Record<string, string> = {};
    for (const g of this.payerGroups()) map[g.id] = g.name;
    return map;
  });

  readonly occurrenceColumns = ['date', 'source', 'payee', 'description', 'amount', 'currency', 'split'];

  readonly occurrencesViewModel = computed<OccurrenceViewModel[]>(() => {
    const peopleMap = this.peopleMap();
    return this.occurrences().map(o => {
      const isIncoming = o.direction === PaymentDirection.Incoming;
      const formattedAmount = this.currencyPipe.transform(o.amount, o.currency) ?? String(o.amount);
      return {
        formattedDate: this.datePipe.transform(o.occurrenceDate, 'd MMM yyyy') ?? o.occurrenceDate,
        sourceName: this.paymentSourcesMap()[o.paymentSourceId] ?? o.paymentSourceId,
        payeeName: this.payeesMap()[o.payeeId] ?? o.payeeId,
        descriptionDisplay: o.description || '—',
        formattedAmount,
        signedAmountDisplay: (isIncoming ? '+ ' : '− ') + formattedAmount,
        directionClass: isIncoming ? 'amount-in' : 'amount-out',
        currency: o.currency,
        splitDisplay: this.formatSplits(o.splits, peopleMap),
      };
    });
  });

  /** Renders a payment's splits as "Alice 50% · Bob 50%" — the owner is just a person like any other. */
  private formatSplits(splits: { personId: string; percentage: number }[], peopleMap: Record<string, string>): string {
    if (splits.length === 0) return '—';
    return splits
      .map(s => {
        const pct = s.percentage % 1 === 0 ? s.percentage.toFixed(0) : s.percentage.toFixed(2);
        return `${peopleMap[s.personId] ?? s.personId} ${pct}%`;
      })
      .join(' · ');
  }

  // Month picker — defaults to current month
  readonly monthControl = new FormControl<Date>(new Date());
  private readonly selectedMonth = signal<Date>(new Date());

  readonly selectedMonthLabel = computed(() =>
    this.datePipe.transform(this.selectedMonth(), 'MMMM yyyy') ?? ''
  );

  private readonly occurrencesResource = rxResource({
    params: () => {
      const date = this.selectedMonth();
      const from = new Date(date.getFullYear(), date.getMonth(), 1);
      const to = new Date(date.getFullYear(), date.getMonth() + 1, 0);
      const prevFrom = new Date(date.getFullYear(), date.getMonth() - 1, 1);
      const prevTo = new Date(date.getFullYear(), date.getMonth(), 0);
      return { from, to, prevFrom, prevTo };
    },
    stream: ({ params: { from, to, prevFrom, prevTo } }) => forkJoin({
      current: this.paymentService.getOccurrences(this.toDateString(from), this.toDateString(to)),
      previous: this.paymentService.getOccurrences(this.toDateString(prevFrom), this.toDateString(prevTo)),
    })
  });

  readonly occurrencesLoading = computed(() => this.occurrencesResource.isLoading());

  readonly occurrences = computed(() => this.occurrencesResource.value()?.current.occurrences ?? []);
  readonly occurrencesSummary = computed(() => this.occurrencesResource.value()?.current.summary ?? []);
  readonly prevOccurrencesSummary = computed(() => this.occurrencesResource.value()?.previous.summary ?? []);

  /** One card per (payer group, currency). Ungrouped payments are not a group — they appear in
   * the People section below, not as a card. */
  readonly groupCardViewModels = computed<GroupCardViewModel[]>(() => {
    const peopleMap = this.peopleMap();
    const overCommitted = this.overCommittedMap();
    const payeesMap = this.payeesMap();
    const payerGroupsMap = this.payerGroupsMap();
    const prevSummary = this.prevOccurrencesSummary();

    const cards: GroupCardViewModel[] = [];
    for (const group of this.occurrencesSummary()) {
      const groupTitle = payerGroupsMap[group.payerGroupId] ?? group.payerGroupId;
      const prevGroup = prevSummary.find(g => g.payerGroupId === group.payerGroupId);

      for (const currencySummary of group.currencies) {
        const income = currencySummary.incoming.totalAmount;
        const outgoing = currencySummary.outgoing.totalAmount;
        const leftOver = income - outgoing;
        const isOverspent = leftOver < 0;

        const prevCurrency = prevGroup?.currencies.find(c => c.currency === currencySummary.currency);
        let delta: string | null = null;
        let deltaState: GroupCardViewModel['deltaState'] = null;
        if (prevCurrency) {
          const deltaAmount = outgoing - prevCurrency.outgoing.totalAmount;
          if (deltaAmount === 0) {
            delta = '— Same as last month';
            deltaState = 'same';
          } else {
            const formatted = this.currencyPipe.transform(Math.abs(deltaAmount), currencySummary.currency) ?? String(Math.abs(deltaAmount));
            delta = deltaAmount > 0 ? `▲ ${formatted} vs last month` : `▼ ${formatted} vs last month`;
            deltaState = deltaAmount > 0 ? 'increase' : 'decrease';
          }
        }

        const payeeSlices: DonutSlice[] = currencySummary.outgoingByPayee.map(p => ({
          label: payeesMap[p.payeeId] ?? p.payeeId,
          amount: p.amount,
        }));
        const donutSlices = !isOverspent && income > 0
          ? [...payeeSlices, ...(leftOver > 0 ? [{ label: 'Left over', amount: leftOver }] : [])]
          : [];

        // Every participant is a person — the user is no longer special-cased; their row is
        // just the split that names them.
        const personRows: PersonRow[] = currencySummary.net.personTotals.map(t => {
          const personIncome = currencySummary.incoming.personTotals.find(p => p.personId === t.personId)?.amount ?? 0;
          const personOutgoing = currencySummary.outgoing.personTotals.find(p => p.personId === t.personId)?.amount ?? 0;
          return this.toPersonRow(
            t.personId,
            peopleMap[t.personId] ?? t.personId,
            personIncome,
            personOutgoing,
            t.amount,
            overCommitted.get(`${t.personId}:${currencySummary.currency}`) ?? false
          );
        });

        const thirdTileLabel = isOverspent ? 'Overspent' : 'Left Over';
        const thirdTileValue = isOverspent
          ? (this.currencyPipe.transform(Math.abs(leftOver), currencySummary.currency) ?? String(Math.abs(leftOver)))
          : (this.currencyPipe.transform(leftOver, currencySummary.currency) ?? String(leftOver));

        cards.push({
          key: `${group.payerGroupId}-${currencySummary.currency}`,
          title: `${groupTitle} — ${currencySummary.currency}`,
          currency: currencySummary.currency,
          incomeDisplay: this.currencyPipe.transform(income, currencySummary.currency) ?? String(income),
          outgoingDisplay: this.currencyPipe.transform(outgoing, currencySummary.currency) ?? String(outgoing),
          thirdTileLabel,
          thirdTileValue,
          isOverspent,
          delta,
          deltaState,
          donutSlices,
          showDonut: donutSlices.length > 1,
          people: personRows,
        });
      }
    }
    return cards;
  });

  private toPersonRow(
    personId: string,
    name: string,
    income: number,
    outgoing: number,
    net: number,
    isOverCommitted: boolean
  ): PersonRow {
    return {
      personId,
      name,
      incomeDisplay: this.currencyPipe.transform(income) ?? String(income),
      outgoingDisplay: this.currencyPipe.transform(outgoing) ?? String(outgoing),
      netDisplay: this.currencyPipe.transform(net) ?? String(net),
      netState: net > 0 ? 'positive' : net < 0 ? 'negative' : 'zero',
      isOverCommitted,
    };
  }

  /** Person over-commitment is a cross-group property; group card rows look it up by person+currency. */
  private readonly overCommittedMap = computed(() => {
    const map = new Map<string, boolean>();
    for (const c of this.occurrencesResource.value()?.current.people ?? []) {
      map.set(`${c.personId}:${c.currency}`, c.isOverCommitted);
    }
    return map;
  });

  /** The global "People" section — income, committed outgoings and headroom, per person per currency. */
  readonly commitmentRows = computed<CommitmentRow[]>(() => {
    const peopleMap = this.peopleMap();
    return (this.occurrencesResource.value()?.current.people ?? []).map(c => ({
      personId: c.personId,
      name: peopleMap[c.personId] ?? c.personId,
      currency: c.currency,
      incomeDisplay: this.currencyPipe.transform(c.income, c.currency) ?? String(c.income),
      committedDisplay: this.currencyPipe.transform(c.committed, c.currency) ?? String(c.committed),
      remainingDisplay: this.currencyPipe.transform(c.remaining, c.currency) ?? String(c.remaining),
      isOverCommitted: c.isOverCommitted,
    }));
  });

  /** Outgoing-only cost breakdown by payment source, grouped by currency — feeds the Cost Breakdown pie. */
  readonly costBreakdownSlices = computed<PayeeSliceGroup[]>(() => {
    const byCurrency = new Map<string, Map<string, number>>();
    for (const o of this.occurrences()) {
      if (o.direction !== PaymentDirection.Outgoing) continue;
      if (!byCurrency.has(o.currency)) byCurrency.set(o.currency, new Map());
      const name = this.paymentSourcesMap()[o.paymentSourceId] ?? o.paymentSourceId;
      const curr = byCurrency.get(o.currency)!;
      curr.set(name, (curr.get(name) ?? 0) + o.amount);
    }
    return [...byCurrency.entries()].map(([currency, totals]) => ({
      currency,
      slices: [...totals.entries()].map(([label, amount]) => ({ label, amount })),
    }));
  });

  /** Outgoing-only payment schedule breakdown by payee, grouped by currency. */
  readonly schedulePayeeSlices = computed<PayeeSliceGroup[]>(() => {
    const byCurrency = new Map<string, Map<string, number>>();
    for (const o of this.occurrences()) {
      if (o.direction !== PaymentDirection.Outgoing) continue;
      if (!byCurrency.has(o.currency)) byCurrency.set(o.currency, new Map());
      const name = this.payeesMap()[o.payeeId] ?? o.payeeId;
      const curr = byCurrency.get(o.currency)!;
      curr.set(name, (curr.get(name) ?? 0) + o.amount);
    }
    return [...byCurrency.entries()].map(([currency, totals]) => ({
      currency,
      slices: [...totals.entries()].map(([label, amount]) => ({ label, amount })),
    }));
  });

  onMonthSelected(date: Date, picker: MatDatepicker<Date>): void {
    this.monthControl.setValue(date);
    this.selectedMonth.set(date);
    picker.close();
  }

  private toDateString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
