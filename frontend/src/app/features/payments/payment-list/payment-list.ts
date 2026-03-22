import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { PayeeService } from '../../../core/services/payee.service';
import { ContactService } from '../../../core/services/contact.service';
import { Payment } from '../../../core/models/payment.model';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { Payee } from '../../../core/models/payee.model';
import { Contact } from '../../../core/models/contact.model';
import { PAYMENT_FREQUENCY_LABELS, PaymentFrequency } from '../../../core/models/payment-frequency.enum';
import { forkJoin } from 'rxjs';

interface PaymentViewModel {
  payeeName: string;
  descriptionDisplay: string;
  formattedAmount: string;
  yourShareDisplay: string;
  frequencyLabel: string;
  formattedStartDate: string;
  formattedEndDate: string;
  _raw: Payment;
}

@Component({
  selector: 'app-payment-list',
  standalone: true,
  providers: [CurrencyPipe, DatePipe, DecimalPipe],
  imports: [
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
    MatButton,
    MatIconButton,
    MatIcon,
    MatCard,
    MatProgressSpinner,
    MatTooltip,
  ],
  templateUrl: './payment-list.html',
  styleUrl: './payment-list.scss'
})
export class PaymentListComponent implements OnInit {
  private readonly paymentService = inject(PaymentService);
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly contactService = inject(ContactService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly currencyPipe = inject(CurrencyPipe);
  private readonly datePipe = inject(DatePipe);

  readonly payments = signal<Payment[]>([]);
  readonly paymentSources = signal<PaymentSource[]>([]);
  readonly payees = signal<Payee[]>([]);
  readonly contacts = signal<Contact[]>([]);
  readonly loading = signal(false);

  readonly displayedColumns = ['payee', 'description', 'amount', 'yourShare', 'frequency', 'startDate', 'endDate', 'actions'];

  private readonly payeesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.payees()) map[p.id] = p.name;
    return map;
  });

  public readonly paymentsViewModel = computed<PaymentViewModel[]>(() =>
    this.payments().map(p => {
      const yourSharePct = Math.max(0, 100 - p.splits.reduce((s, x) => s + x.percentage, 0));
      return {
        payeeName: this.payeesMap()[p.payeeId] ?? p.payeeId,
        descriptionDisplay: p.description || '—',
        formattedAmount: this.currencyPipe.transform(p.amount, p.currency) ?? String(p.amount),
        yourShareDisplay: `${yourSharePct % 1 === 0 ? yourSharePct.toFixed(0) : yourSharePct.toFixed(2)}%`,
        frequencyLabel: PAYMENT_FREQUENCY_LABELS[p.frequency as PaymentFrequency] ?? String(p.frequency),
        formattedStartDate: this.datePipe.transform(p.startDate, 'mediumDate') ?? p.startDate,
        formattedEndDate: p.endDate ? (this.datePipe.transform(p.endDate, 'mediumDate') ?? p.endDate) : '—',
        _raw: p,
      };
    })
  );

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    forkJoin({
      payments: this.paymentService.getAll(),
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
      contacts: this.contactService.getAll(),
    }).subscribe({
      next: ({ payments, paymentSources, payees, contacts }) => {
        this.payments.set(payments);
        this.paymentSources.set(paymentSources);
        this.payees.set(payees);
        this.contacts.set(contacts);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load data', 'Close', { duration: 3000 });
      }
    });
  }

  async openCreateDialog(): Promise<void> {
    const { PaymentFormDialogComponent } = await import('../payment-form-dialog/payment-form-dialog');
    const ref = this.dialog.open(PaymentFormDialogComponent, {
      width: '520px',
      data: { paymentSources: this.paymentSources(), payees: this.payees(), contacts: this.contacts() }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.paymentService.create(result).subscribe({
          next: () => {
            this.snackBar.open('Payment created', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to create payment', 'Close', { duration: 3000 })
        });
      }
    });
  }

  async openEditDialog(payment: Payment): Promise<void> {
    const { PaymentFormDialogComponent } = await import('../payment-form-dialog/payment-form-dialog');
    const ref = this.dialog.open(PaymentFormDialogComponent, {
      width: '520px',
      data: { payment, paymentSources: this.paymentSources(), payees: this.payees(), contacts: this.contacts() }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.paymentService.update(payment.id, result).subscribe({
          next: () => {
            this.snackBar.open('Payment updated', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to update payment', 'Close', { duration: 3000 })
        });
      }
    });
  }

  async deletePayment(payment: Payment): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payment', message: 'Are you sure you want to delete this payment?' }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.paymentService.delete(payment.id).subscribe({
          next: () => {
            this.snackBar.open('Payment deleted', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to delete payment', 'Close', { duration: 3000 })
        });
      }
    });
  }
}
