import { Component, inject, OnInit, signal, computed } from '@angular/core';
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
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentService } from '../../core/services/payment.service';
import { ContactService } from '../../core/services/contact.service';
import { OccurrenceSummary, PaymentOccurrence } from '../../core/models/payment.model';
import { Payee } from '../../core/models/payee.model';
import { PaymentSource } from '../../core/models/payment-source.model';
import { Contact } from '../../core/models/contact.model';
import { forkJoin } from 'rxjs';
import { SourcePieChartComponent, PieSlice } from './source-pie-chart';

interface OccurrenceViewModel {
  formattedDate: string;
  sourceName: string;
  payeeName: string;
  descriptionDisplay: string;
  formattedAmount: string;
  currency: string;
  yourShareDisplay: string;
}

interface SummaryContactRow {
  name: string;
  amount: string;
}

interface PaymentSourceSummaryVm {
  sourceName: string;
  totalAmount: string;
  userTotal: string;
  contacts: SummaryContactRow[];
}

interface SummaryViewModel {
  currency: string;
  totalAmount: string;
  userTotal: string;
  delta: string | null;
  deltaState: 'increase' | 'decrease' | 'same' | null;
  contacts: SummaryContactRow[];
  byPaymentSource: PaymentSourceSummaryVm[];
  pieSlices: PieSlice[];
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
    ReactiveFormsModule,
    SourcePieChartComponent,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly paymentService = inject(PaymentService);
  private readonly contactService = inject(ContactService);
  private readonly currencyPipe = inject(CurrencyPipe);
  private readonly datePipe = inject(DatePipe);

  readonly loading = signal(false);

  readonly occurrences = signal<PaymentOccurrence[]>([]);
  readonly occurrencesSummary = signal<OccurrenceSummary[]>([]);
  readonly prevOccurrencesSummary = signal<OccurrenceSummary[]>([]);
  readonly occurrencesLoading = signal(false);
  readonly payees = signal<Payee[]>([]);
  readonly contacts = signal<Contact[]>([]);

  private readonly payeesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.payees()) map[p.id] = p.name;
    return map;
  });

  readonly paymentSources = signal<PaymentSource[]>([]);

  private readonly paymentSourcesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const ps of this.paymentSources()) map[ps.id] = ps.name;
    return map;
  });

  private readonly contactsMap = computed(() => {
    const map: Record<string, string> = {};
    for (const c of this.contacts()) map[c.id] = c.name;
    return map;
  });

  readonly occurrenceColumns = ['date', 'source', 'payee', 'description', 'amount', 'currency', 'yourShare'];

  readonly occurrencesViewModel = computed<OccurrenceViewModel[]>(() =>
    this.occurrences().map(o => {
      const pct = o.userShare.percentage;
      return {
        formattedDate: this.datePipe.transform(o.occurrenceDate, 'd MMM yyyy') ?? o.occurrenceDate,
        sourceName: this.paymentSourcesMap()[o.paymentSourceId] ?? o.paymentSourceId,
        payeeName: this.payeesMap()[o.payeeId] ?? o.payeeId,
        descriptionDisplay: o.description || '—',
        formattedAmount: this.currencyPipe.transform(o.amount, o.currency) ?? String(o.amount),
        currency: o.currency,
        yourShareDisplay: `${pct % 1 === 0 ? pct.toFixed(0) : pct.toFixed(2)}%`,
      };
    })
  );

  readonly summaryViewModel = computed<SummaryViewModel[]>(() => {
    const contactsMap = this.contactsMap();
    const paymentSourcesMap = this.paymentSourcesMap();
    const prevSummary = this.prevOccurrencesSummary();

    return this.occurrencesSummary().map(s => {
      const prev = prevSummary.find(p => p.currency === s.currency);
      const deltaAmount = prev != null ? s.totalAmount - prev.totalAmount : null;
      let delta: string | null = null;
      let deltaState: SummaryViewModel['deltaState'] = null;
      if (deltaAmount !== null) {
        if (deltaAmount === 0) {
          delta = '— Same as last month';
          deltaState = 'same';
        } else {
          const abs = Math.abs(deltaAmount);
          const formatted = this.currencyPipe.transform(abs, s.currency) ?? String(abs);
          delta = deltaAmount > 0 ? `▲ ${formatted} vs last month` : `▼ ${formatted} vs last month`;
          deltaState = deltaAmount > 0 ? 'increase' : 'decrease';
        }
      }

      return {
        currency: s.currency,
        totalAmount: this.currencyPipe.transform(s.totalAmount, s.currency) ?? String(s.totalAmount),
        userTotal: this.currencyPipe.transform(s.userTotal, s.currency) ?? String(s.userTotal),
        delta,
        deltaState,
        contacts: s.contactTotals.map(c => ({
          name: contactsMap[c.contactId] ?? c.contactId,
          amount: this.currencyPipe.transform(c.amount, s.currency) ?? String(c.amount),
        })),
        byPaymentSource: s.byPaymentSource.map(ps => ({
          sourceName: paymentSourcesMap[ps.paymentSourceId] ?? ps.paymentSourceId,
          totalAmount: this.currencyPipe.transform(ps.totalAmount, s.currency) ?? String(ps.totalAmount),
          userTotal: this.currencyPipe.transform(ps.userTotal, s.currency) ?? String(ps.userTotal),
          contacts: ps.contactTotals.map(c => ({
            name: contactsMap[c.contactId] ?? c.contactId,
            amount: this.currencyPipe.transform(c.amount, s.currency) ?? String(c.amount),
          })),
        })),
        pieSlices: s.byPaymentSource.map(ps => ({
          label: paymentSourcesMap[ps.paymentSourceId] ?? ps.paymentSourceId,
          amount: ps.totalAmount,
        })),
      };
    });
  });

  // Month picker — defaults to current month
  readonly monthControl = new FormControl<Date>(new Date());
  private readonly selectedMonth = signal<Date>(new Date());

  readonly selectedMonthLabel = computed(() =>
    this.datePipe.transform(this.selectedMonth(), 'MMMM yyyy') ?? ''
  );

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
      contacts: this.contactService.getAll(),
    }).subscribe({
      next: ({ paymentSources, payees, contacts }) => {
        this.payees.set(payees);
        this.paymentSources.set(paymentSources);
        this.contacts.set(contacts);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.loadOccurrences(new Date());
  }

  onMonthSelected(date: Date, picker: MatDatepicker<Date>): void {
    this.monthControl.setValue(date);
    this.selectedMonth.set(date);
    picker.close();
    this.loadOccurrences(date);
  }

  private loadOccurrences(date: Date): void {
    const from = new Date(date.getFullYear(), date.getMonth(), 1);
    const to = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    const prevFrom = new Date(date.getFullYear(), date.getMonth() - 1, 1);
    const prevTo = new Date(date.getFullYear(), date.getMonth(), 0);

    this.occurrencesLoading.set(true);
    forkJoin({
      current: this.paymentService.getOccurrences(this.toDateString(from), this.toDateString(to)),
      previous: this.paymentService.getOccurrences(this.toDateString(prevFrom), this.toDateString(prevTo)),
    }).subscribe({
      next: ({ current, previous }) => {
        this.occurrences.set(current.occurrences);
        this.occurrencesSummary.set(current.summary);
        this.prevOccurrencesSummary.set(previous.summary);
        this.occurrencesLoading.set(false);
      },
      error: () => this.occurrencesLoading.set(false),
    });
  }

  private toDateString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
