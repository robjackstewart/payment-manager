import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatCard, MatCardHeader, MatCardAvatar, MatCardTitle, MatCardSubtitle, MatCardContent, MatCardActions } from '@angular/material/card';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentService } from '../../core/services/payment.service';
import { PaymentOccurrence } from '../../core/models/payment.model';
import { Payee } from '../../core/models/payee.model';
import { PaymentSource } from '../../core/models/payment-source.model';
import { forkJoin } from 'rxjs';

interface OccurrenceViewModel {
  formattedDate: string;
  sourceName: string;
  payeeName: string;
  descriptionDisplay: string;
  formattedAmount: string;
  currency: string;
  yourShareDisplay: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  providers: [CurrencyPipe, DatePipe, provideNativeDateAdapter()],
  imports: [
    RouterLink,
    MatCard,
    MatCardHeader,
    MatCardAvatar,
    MatCardTitle,
    MatCardSubtitle,
    MatCardContent,
    MatCardActions,
    MatButton,
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
    ReactiveFormsModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly paymentService = inject(PaymentService);
  private readonly currencyPipe = inject(CurrencyPipe);
  private readonly datePipe = inject(DatePipe);

  readonly loading = signal(false);
  readonly paymentSourceCount = signal(0);
  readonly payeeCount = signal(0);
  readonly paymentCount = signal(0);

  readonly occurrences = signal<PaymentOccurrence[]>([]);
  readonly occurrencesLoading = signal(false);
  readonly payees = signal<Payee[]>([]);

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

  // Month picker — defaults to current month
  readonly monthControl = new FormControl<Date>(new Date());

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
      payments: this.paymentService.getAll(),
    }).subscribe({
      next: ({ paymentSources, payees, payments }) => {
        this.paymentSourceCount.set(paymentSources.length);
        this.payeeCount.set(payees.length);
        this.paymentCount.set(payments.length);
        this.payees.set(payees);
        this.paymentSources.set(paymentSources);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.loadOccurrences(new Date());
  }

  onMonthSelected(date: Date, picker: MatDatepicker<Date>): void {
    this.monthControl.setValue(date);
    picker.close();
    this.loadOccurrences(date);
  }

  private loadOccurrences(date: Date): void {
    const from = new Date(date.getFullYear(), date.getMonth(), 1);
    const to = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    const fromStr = this.toDateString(from);
    const toStr = this.toDateString(to);

    this.occurrencesLoading.set(true);
    this.paymentService.getOccurrences(fromStr, toStr).subscribe({
      next: occurrences => {
        this.occurrences.set(occurrences);
        this.occurrencesLoading.set(false);
      },
      error: () => this.occurrencesLoading.set(false)
    });
  }

  private toDateString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
