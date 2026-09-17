import { Component, computed, inject, input, signal } from '@angular/core';
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
import { PersonService } from '../../../core/services/person.service';
import { PayerGroupService } from '../../../core/services/payer-group.service';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { AddPaymentSplitsRequest, AddPaymentValueRequest, Payment, UpdatePaymentRequest } from '../../../core/models/payment.model';
import { PAYMENT_FREQUENCY_LABELS, PaymentFrequency } from '../../../core/models/payment-frequency.enum';
import { PaymentDirection } from '../../../core/models/payment-direction.enum';
import { firstValueFrom, forkJoin, of } from 'rxjs';

interface PaymentViewModel {
  payeeName: string;
  groupName: string;
  descriptionDisplay: string;
  formattedAmount: string;
  splitDisplay: string;
  frequencyLabel: string;
  formattedStartDate: string;
  formattedEndDate: string;
  _raw: Payment;
}

@Component({
  selector: 'app-payment-list',
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
  private readonly personService = inject(PersonService);
  private readonly payerGroupService = inject(PayerGroupService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly currencyPipe = inject(CurrencyPipe);
  private readonly datePipe = inject(DatePipe);
  readonly breakpointService = inject(BreakpointService);

  /** Which payments this page manages — set via route data (see app.routes.ts). */
  readonly direction = input<PaymentDirection>(PaymentDirection.Outgoing);

  readonly isIncoming = computed(() => this.direction() === PaymentDirection.Incoming);
  readonly newButtonLabel = computed(() => this.isIncoming() ? 'New Income' : 'New Payment');
  readonly emptyStateMessage = computed(() => this.isIncoming()
    ? 'No income yet. Create one to get started.'
    : 'No payments yet. Create one to get started.');
  readonly payeeColumnLabel = computed(() => this.isIncoming() ? 'Paid By' : 'Paid To');

  private readonly reloadTrigger = signal(0);

  private readonly allDataResource = rxResource({
    params: () => ({ trigger: this.reloadTrigger(), direction: this.direction() }),
    stream: ({ params }) => forkJoin({
      payments: this.paymentService.getAll(params.direction),
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
      people: this.personService.getAll(),
      payerGroups: this.payerGroupService.getAll(),
    })
  });

  readonly isLoading = computed(() => this.allDataResource.isLoading());

  private readonly allData = computed(() => this.allDataResource.value());

  readonly payments = computed(() => this.allData()?.payments ?? []);
  readonly paymentSources = computed(() => this.allData()?.paymentSources ?? []);
  readonly payees = computed(() => this.allData()?.payees ?? []);
  readonly people = computed(() => this.allData()?.people ?? []);
  readonly payerGroups = computed(() => this.allData()?.payerGroups ?? []);

  readonly displayedColumns = ['payee', 'group', 'description', 'amount', 'split', 'frequency', 'startDate', 'endDate', 'actions'];

  private readonly payeesMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.payees()) map[p.id] = p.name;
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

  public readonly paymentsViewModel = computed<PaymentViewModel[]>(() => {
    const peopleMap = this.peopleMap();
    return this.payments().map(p => {
      const groupName = p.payerGroupId ? (this.payerGroupsMap()[p.payerGroupId] ?? p.payerGroupId) : '—';
      return {
        payeeName: this.payeesMap()[p.payeeId] ?? p.payeeId,
        groupName,
        descriptionDisplay: p.description || '—',
        formattedAmount: this.currencyPipe.transform(p.currentAmount, p.currency) ?? String(p.currentAmount),
        splitDisplay: this.formatSplits(p.splits, peopleMap),
        frequencyLabel: PAYMENT_FREQUENCY_LABELS[p.frequency as PaymentFrequency] ?? String(p.frequency),
        formattedStartDate: this.datePipe.transform(p.startDate, 'mediumDate') ?? p.startDate,
        formattedEndDate: p.endDate ? (this.datePipe.transform(p.endDate, 'mediumDate') ?? p.endDate) : '—',
        _raw: p,
      };
    });
  });

  /** Renders a payment's splits as "Alice 50% · Bob 50%". */
  private formatSplits(splits: { personId: string; percentage: number }[], peopleMap: Record<string, string>): string {
    if (splits.length === 0) return '—';
    return splits
      .map(s => {
        const pct = s.percentage % 1 === 0 ? s.percentage.toFixed(0) : s.percentage.toFixed(2);
        return `${peopleMap[s.personId] ?? s.personId} ${pct}%`;
      })
      .join(' · ');
  }

  private reload(): void { this.reloadTrigger.update(n => n + 1); }

  async openCreateDialog(): Promise<void> {
    const { PaymentFormDialogComponent } = await import('../payment-form-dialog/payment-form-dialog');
    const ref = this.dialog.open(PaymentFormDialogComponent, {
      width: '520px',
      data: { direction: this.direction(), paymentSources: this.paymentSources(), payees: this.payees(), people: this.people(), payerGroups: this.payerGroups() }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.paymentService.create(result));
      this.snackBar.open(this.isIncoming() ? 'Income created' : 'Payment created', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open(this.isIncoming() ? 'Failed to create income' : 'Failed to create payment', 'Close', { duration: 3000 });
    }
  }

  async openEditDialog(payment: Payment): Promise<void> {
    const { PaymentFormDialogComponent } = await import('../payment-form-dialog/payment-form-dialog');
    const ref = this.dialog.open(PaymentFormDialogComponent, {
      width: '520px',
      data: { payment, direction: this.direction(), paymentSources: this.paymentSources(), payees: this.payees(), people: this.people(), payerGroups: this.payerGroups() }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      const {
        metadataRequest,
        valuesToUpsert,
        valuesToRemove,
        splitVersionsToUpsert,
        splitVersionsToRemove,
      }: {
        metadataRequest: UpdatePaymentRequest;
        valuesToUpsert: AddPaymentValueRequest[];
        valuesToRemove: string[];
        splitVersionsToUpsert: AddPaymentSplitsRequest[];
        splitVersionsToRemove: string[];
      } = result;
      const update$ = this.paymentService.update(payment.id, metadataRequest);
      const removes$ = (valuesToRemove ?? []).length
        ? (valuesToRemove ?? []).map(d => this.paymentService.removeValue(payment.id, d))
        : [of(null)];
      const valueUpserts$ = valuesToUpsert.length
        ? valuesToUpsert.map(v => this.paymentService.addValue(payment.id, v))
        : [of(null)];
      const splitVersionRemoves$ = (splitVersionsToRemove ?? []).length
        ? (splitVersionsToRemove ?? []).map(d => this.paymentService.removeSplitVersion(payment.id, d))
        : [of(null)];
      const splitVersionUpserts$ = (splitVersionsToUpsert ?? []).length
        ? (splitVersionsToUpsert ?? []).map(v => this.paymentService.addSplitVersion(payment.id, v))
        : [of(null)];
      await firstValueFrom(forkJoin([
        update$,
        ...removes$,
        ...valueUpserts$,
        ...splitVersionRemoves$,
        ...splitVersionUpserts$,
      ]));
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
