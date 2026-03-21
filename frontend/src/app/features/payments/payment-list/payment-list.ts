import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { PayeeService } from '../../../core/services/payee.service';
import { Payment } from '../../../core/models/payment.model';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { Payee } from '../../../core/models/payee.model';
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
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './payment-list.html',
  styleUrl: './payment-list.scss'
})
export class PaymentListComponent implements OnInit {
  private readonly paymentService = inject(PaymentService);
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly payments = signal<Payment[]>([]);
  readonly paymentSources = signal<PaymentSource[]>([]);
  readonly payees = signal<Payee[]>([]);
  readonly loading = signal(false);

  readonly displayedColumns = ['payee', 'description', 'amount', 'frequency', 'startDate', 'endDate', 'actions'];

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

  private load(): void {
    this.loading.set(true);
    forkJoin({
      payments: this.paymentService.getAll(),
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
    }).subscribe({
      next: ({ payments, paymentSources, payees }) => {
        this.payments.set(payments);
        this.paymentSources.set(paymentSources);
        this.payees.set(payees);
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
      width: '500px',
      data: { paymentSources: this.paymentSources(), payees: this.payees() }
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
      width: '500px',
      data: { payment, paymentSources: this.paymentSources(), payees: this.payees() }
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
