import { Component, inject, OnInit, signal, computed } from '@angular/core';
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
import { PaymentFormDialogComponent } from '../payment-form-dialog/payment-form-dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-payment-list',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
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

  readonly payments = signal<Payment[]>([]);
  readonly paymentSources = signal<PaymentSource[]>([]);
  readonly payees = signal<Payee[]>([]);
  readonly contacts = signal<Contact[]>([]);
  readonly loading = signal(false);

  readonly displayedColumns = ['payee', 'description', 'amount', 'yourShare', 'frequency', 'startDate', 'endDate', 'actions'];

  readonly payeesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.payees()) map[p.id] = p.name;
    return map;
  });

  ngOnInit(): void {
    this.load();
  }

  getFrequencyLabel(freq: number): string {
    return PAYMENT_FREQUENCY_LABELS[freq as PaymentFrequency] ?? String(freq);
  }

  getYourShare(payment: Payment): number {
    const total = payment.splits.reduce((sum, s) => sum + s.percentage, 0);
    return Math.max(0, 100 - total);
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

  openCreateDialog(): void {
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

  openEditDialog(payment: Payment): void {
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

  deletePayment(payment: Payment): void {
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
