import { Component, inject, OnInit, signal } from '@angular/core';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { PayeeService } from '../../../core/services/payee.service';
import { Payee } from '../../../core/models/payee.model';

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
export class PayeeListComponent implements OnInit {
  private readonly payeeService = inject(PayeeService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly payees = signal<Payee[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.payeeService.getAll().subscribe({
      next: payees => {
        this.payees.set(payees);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load payees', 'Close', { duration: 3000 });
      }
    });
  }

  async openCreateDialog(): Promise<void> {
    const { PayeeFormDialogComponent } = await import('../payee-form-dialog/payee-form-dialog');
    const ref = this.dialog.open(PayeeFormDialogComponent, { width: '450px', data: {} });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.payeeService.create(result).subscribe({
          next: () => {
            this.snackBar.open('Payee created', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to create payee', 'Close', { duration: 3000 })
        });
      }
    });
  }

  async openEditDialog(payee: Payee): Promise<void> {
    const { PayeeFormDialogComponent } = await import('../payee-form-dialog/payee-form-dialog');
    const ref = this.dialog.open(PayeeFormDialogComponent, {
      width: '450px',
      data: { payee }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.payeeService.update(payee.id, result).subscribe({
          next: () => {
            this.snackBar.open('Payee updated', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to update payee', 'Close', { duration: 3000 })
        });
      }
    });
  }

  async deletePayee(payee: Payee): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payee', message: `Are you sure you want to delete "${payee.name}"?` }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.payeeService.delete(payee.id).subscribe({
          next: () => {
            this.snackBar.open('Payee deleted', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to delete payee', 'Close', { duration: 3000 })
        });
      }
    });
  }
}
