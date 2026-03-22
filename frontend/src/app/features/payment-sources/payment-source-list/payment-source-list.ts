import { Component, inject, OnInit, signal } from '@angular/core';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { PaymentSourceFormDialogComponent } from '../payment-source-form-dialog/payment-source-form-dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog';

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
    MatProgressSpinner,
    MatTooltip,
  ],
  templateUrl: './payment-source-list.html',
  styleUrl: './payment-source-list.scss'
})
export class PaymentSourceListComponent implements OnInit {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly paymentSources = signal<PaymentSource[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.paymentSourceService.getAll().subscribe({
      next: sources => {
        this.paymentSources.set(sources);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load payment sources', 'Close', { duration: 3000 });
      }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(PaymentSourceFormDialogComponent, { width: '450px', data: {} });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.paymentSourceService.create(result).subscribe({
          next: () => {
            this.snackBar.open('Payment source created', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to create payment source', 'Close', { duration: 3000 })
        });
      }
    });
  }

  openEditDialog(ps: PaymentSource): void {
    const ref = this.dialog.open(PaymentSourceFormDialogComponent, {
      width: '450px',
      data: { paymentSource: ps }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.paymentSourceService.update(ps.id, result).subscribe({
          next: () => {
            this.snackBar.open('Payment source updated', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to update payment source', 'Close', { duration: 3000 })
        });
      }
    });
  }

  deletePaymentSource(ps: PaymentSource): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payment Source', message: `Are you sure you want to delete "${ps.name}"?` }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.paymentSourceService.delete(ps.id).subscribe({
          next: () => {
            this.snackBar.open('Payment source deleted', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to delete payment source', 'Close', { duration: 3000 })
        });
      }
    });
  }
}
