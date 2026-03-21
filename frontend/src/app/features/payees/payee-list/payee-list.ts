import { Component, inject, OnInit, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PayeeService } from '../../../core/services/payee.service';
import { Payee } from '../../../core/models/payee.model';
import { PayeeFormDialogComponent } from '../payee-form-dialog/payee-form-dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-payee-list',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
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

  openCreateDialog(): void {
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

  openEditDialog(payee: Payee): void {
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

  deletePayee(payee: Payee): void {
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
