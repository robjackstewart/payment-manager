import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { rxResource } from '@angular/core/rxjs-interop';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { PayeeService } from '../../../core/services/payee.service';
import { ContactService } from '../../../core/services/contact.service';
import { AddPaymentValueRequest, Payment, UpdatePaymentRequest } from '../../../core/models/payment.model';
import { PAYMENT_FREQUENCY_LABELS, PaymentFrequency } from '../../../core/models/payment-frequency.enum';
import { firstValueFrom, forkJoin, of } from 'rxjs';

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
    MatCardContent,
    MatProgressSpinner,
    MatTooltip,
  ],
  templateUrl: './payment-list.html',
  styleUrl: './payment-list.scss'
})
export class PaymentListComponent {
  private readonly paymentService = inject(PaymentService);
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly contactService = inject(ContactService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly currencyPipe = inject(CurrencyPipe);
  private readonly datePipe = inject(DatePipe);

  private readonly reloadTrigger = signal(0);

  private readonly allDataResource = rxResource({
    params: () => this.reloadTrigger(),
    stream: () => forkJoin({
      payments: this.paymentService.getAll(),
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
      contacts: this.contactService.getAll(),
    })
  });

  readonly isLoading = computed(() => this.allDataResource.isLoading());

  private readonly allData = computed(() => this.allDataResource.value());

  readonly payments = computed(() => this.allData()?.payments ?? []);
  readonly paymentSources = computed(() => this.allData()?.paymentSources ?? []);
  readonly payees = computed(() => this.allData()?.payees ?? []);
  readonly contacts = computed(() => this.allData()?.contacts ?? []);

  readonly displayedColumns = ['payee', 'description', 'amount', 'yourShare', 'frequency', 'startDate', 'endDate', 'actions'];

  private readonly payeesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.payees()) map[p.id] = p.name;
    return map;
  });

  public readonly paymentsViewModel = computed<PaymentViewModel[]>(() =>
    this.payments().map(p => {
      const pct = p.userShare.percentage;
      return {
        payeeName: this.payeesMap()[p.payeeId] ?? p.payeeId,
        descriptionDisplay: p.description || '—',
        formattedAmount: this.currencyPipe.transform(p.currentAmount, p.currency) ?? String(p.currentAmount),
        yourShareDisplay: `${pct % 1 === 0 ? pct.toFixed(0) : pct.toFixed(2)}%`,
        frequencyLabel: PAYMENT_FREQUENCY_LABELS[p.frequency as PaymentFrequency] ?? String(p.frequency),
        formattedStartDate: this.datePipe.transform(p.startDate, 'mediumDate') ?? p.startDate,
        formattedEndDate: p.endDate ? (this.datePipe.transform(p.endDate, 'mediumDate') ?? p.endDate) : '—',
        _raw: p,
      };
    })
  );

  private reload(): void { this.reloadTrigger.update(n => n + 1); }

  async openCreateDialog(): Promise<void> {
    const { PaymentFormDialogComponent } = await import('../payment-form-dialog/payment-form-dialog');
    const ref = this.dialog.open(PaymentFormDialogComponent, {
      width: '520px',
      data: { paymentSources: this.paymentSources(), payees: this.payees(), contacts: this.contacts() }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.paymentService.create(result));
      this.snackBar.open('Payment created', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to create payment', 'Close', { duration: 3000 });
    }
  }

  async openEditDialog(payment: Payment): Promise<void> {
    const { PaymentFormDialogComponent } = await import('../payment-form-dialog/payment-form-dialog');
    const ref = this.dialog.open(PaymentFormDialogComponent, {
      width: '520px',
      data: { payment, paymentSources: this.paymentSources(), payees: this.payees(), contacts: this.contacts() }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      const { metadataRequest, valuesToUpsert, valuesToRemove }: { metadataRequest: UpdatePaymentRequest; valuesToUpsert: AddPaymentValueRequest[]; valuesToRemove: string[] } = result;
      const update$ = this.paymentService.update(payment.id, metadataRequest);
      const removes$ = (valuesToRemove ?? []).length
        ? (valuesToRemove ?? []).map(d => this.paymentService.removeValue(payment.id, d))
        : [of(null)];
      const valueUpserts$ = valuesToUpsert.length
        ? valuesToUpsert.map(v => this.paymentService.addValue(payment.id, v))
        : [of(null)];
      await firstValueFrom(forkJoin([update$, ...removes$, ...valueUpserts$]));
      this.snackBar.open('Payment updated', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to update payment', 'Close', { duration: 3000 });
    }
  }

  async deletePayment(payment: Payment): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payment', message: 'Are you sure you want to delete this payment?' }
    });
    const confirmed = await firstValueFrom(ref.afterClosed());
    if (!confirmed) return;
    try {
      await firstValueFrom(this.paymentService.delete(payment.id));
      this.snackBar.open('Payment deleted', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to delete payment', 'Close', { duration: 3000 });
    }
  }
}
