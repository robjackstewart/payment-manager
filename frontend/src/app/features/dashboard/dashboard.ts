import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepicker, MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentService } from '../../core/services/payment.service';
import { PaymentOccurrence } from '../../core/models/payment.model';
import { Payee } from '../../core/models/payee.model';
import { PaymentSource } from '../../core/models/payment-source.model';
import { PAYMENT_FREQUENCY_LABELS, PaymentFrequency } from '../../core/models/payment-frequency.enum';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    ReactiveFormsModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly paymentService = inject(PaymentService);

  readonly loading = signal(false);
  readonly paymentSourceCount = signal(0);
  readonly payeeCount = signal(0);
  readonly paymentCount = signal(0);

  readonly occurrences = signal<PaymentOccurrence[]>([]);
  readonly occurrencesLoading = signal(false);
  readonly payees = signal<Payee[]>([]);

  readonly payeesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.payees()) map[p.id] = p.name;
    return map;
  });

  readonly paymentSources = signal<PaymentSource[]>([]);

  readonly paymentSourcesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const ps of this.paymentSources()) map[ps.id] = ps.name;
    return map;
  });

  readonly occurrenceColumns = ['date', 'source', 'payee', 'description', 'amount', 'currency', 'yourShare'];

  getYourShare(occurrence: PaymentOccurrence): number {
    const total = occurrence.splits.reduce((sum, s) => sum + s.percentage, 0);
    return Math.max(0, 100 - total);
  }

  // Month picker — defaults to current month
  readonly monthControl = new FormControl<Date>(new Date());

  getFrequencyLabel(freq: number): string {
    return PAYMENT_FREQUENCY_LABELS[freq as PaymentFrequency] ?? String(freq);
  }

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
