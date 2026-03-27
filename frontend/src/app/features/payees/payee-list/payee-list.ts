import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { firstValueFrom } from 'rxjs';
import { PayeeService } from '../../../core/services/payee.service';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { Payee } from '../../../core/models/payee.model';
import { LOADING, LoadingState, isLoaded } from '../../../core/utils/loading.utils';

@Component({
  selector: 'app-payee-list',
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
  templateUrl: './payee-list.html',
  styleUrl: './payee-list.scss'
})
export class PayeeListComponent {
  private readonly payeeService = inject(PayeeService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly breakpointService = inject(BreakpointService);

  private readonly reloadTrigger = signal(0);

  private readonly payeesResource = rxResource({
    params: () => this.reloadTrigger(),
    stream: () => this.payeeService.getAll()
  });

  readonly payees = computed<LoadingState<Payee[]>>(() =>
    this.payeesResource.isLoading() ? LOADING : (this.payeesResource.value() ?? [])
  );
  readonly loading = computed(() => this.payees() === LOADING);
  readonly payeesForTable = computed(() => isLoaded(this.payees()) ? this.payees() as Payee[] : []);

  readonly displayedColumns = ['name', 'actions'];

  private reload(): void { this.reloadTrigger.update(n => n + 1); }

  async openCreateDialog(): Promise<void> {
    const { PayeeFormDialogComponent } = await import('../payee-form-dialog/payee-form-dialog');
    const ref = this.dialog.open(PayeeFormDialogComponent, { width: '450px', data: {} });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.payeeService.create(result));
      this.snackBar.open('Payee created', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to create payee', 'Close', { duration: 3000 });
    }
  }

  async openEditDialog(payee: Payee): Promise<void> {
    const { PayeeFormDialogComponent } = await import('../payee-form-dialog/payee-form-dialog');
    const ref = this.dialog.open(PayeeFormDialogComponent, { width: '450px', data: { payee } });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.payeeService.update(payee.id, result));
      this.snackBar.open('Payee updated', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to update payee', 'Close', { duration: 3000 });
    }
  }

  async deletePayee(payee: Payee): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payee', message: `Are you sure you want to delete "${payee.name}"?` }
    });
    const confirmed = await firstValueFrom(ref.afterClosed());
    if (!confirmed) return;
    try {
      await firstValueFrom(this.payeeService.delete(payee.id));
      this.snackBar.open('Payee deleted', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to delete payee', 'Close', { duration: 3000 });
    }
  }
}
