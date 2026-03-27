import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { LOADING, LoadingState, isLoaded } from '../../../core/utils/loading.utils';

@Component({
  selector: 'app-payment-source-list',
  standalone: true,
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
  templateUrl: './payment-source-list.html',
  styleUrl: './payment-source-list.scss'
})
export class PaymentSourceListComponent {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly breakpointService = inject(BreakpointService);

  private readonly reloadTrigger = signal(0);

  private readonly paymentSourcesResource = rxResource({
    params: () => this.reloadTrigger(),
    stream: () => this.paymentSourceService.getAll()
  });

  readonly paymentSources = computed<LoadingState<PaymentSource[]>>(() =>
    this.paymentSourcesResource.isLoading() ? LOADING : (this.paymentSourcesResource.value() ?? [])
  );

  readonly isLoading = computed(() => this.paymentSources() === LOADING);

  readonly paymentSourcesForTable = computed(() =>
    isLoaded(this.paymentSources()) ? this.paymentSources() as PaymentSource[] : []
  );

  readonly displayedColumns = ['name', 'actions'];

  private reload(): void { this.reloadTrigger.update(n => n + 1); }

  async openCreateDialog(): Promise<void> {
    const { PaymentSourceFormDialogComponent } = await import('../payment-source-form-dialog/payment-source-form-dialog');
    const ref = this.dialog.open(PaymentSourceFormDialogComponent, { width: '450px', data: {} });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.paymentSourceService.create(result));
      this.snackBar.open('Payment source created', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to create payment source', 'Close', { duration: 3000 });
    }
  }

  async openEditDialog(ps: PaymentSource): Promise<void> {
    const { PaymentSourceFormDialogComponent } = await import('../payment-source-form-dialog/payment-source-form-dialog');
    const ref = this.dialog.open(PaymentSourceFormDialogComponent, {
      width: '450px',
      data: { paymentSource: ps }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.paymentSourceService.update(ps.id, result));
      this.snackBar.open('Payment source updated', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to update payment source', 'Close', { duration: 3000 });
    }
  }

  async deletePaymentSource(ps: PaymentSource): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payment Source', message: `Are you sure you want to delete "${ps.name}"?` }
    });
    const confirmed = await firstValueFrom(ref.afterClosed());
    if (!confirmed) return;
    try {
      await firstValueFrom(this.paymentSourceService.delete(ps.id));
      this.snackBar.open('Payment source deleted', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to delete payment source', 'Close', { duration: 3000 });
    }
  }
}
