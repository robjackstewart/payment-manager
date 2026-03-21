import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { UserService } from '../../../core/services/user.service';
import { UserContextService } from '../../../core/services/user-context.service';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { User } from '../../../core/models/user.model';
import { PaymentSourceFormDialogComponent } from '../payment-source-form-dialog/payment-source-form-dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-payment-source-list',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatSelectModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './payment-source-list.html',
  styleUrl: './payment-source-list.scss'
})
export class PaymentSourceListComponent implements OnInit {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly userService = inject(UserService);
  readonly userContext = inject(UserContextService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly paymentSources = signal<PaymentSource[]>([]);
  readonly users = signal<User[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'actions'];

  constructor() {
    effect(() => {
      const user = this.userContext.selectedUser();
      if (user) this.loadSources(user.id);
      else this.paymentSources.set([]);
    });
  }

  ngOnInit(): void {
    this.userService.getAll().subscribe(users => this.users.set(users));
  }

  private loadSources(userId: string): void {
    this.loading.set(true);
    this.paymentSourceService.getAll(userId).subscribe({
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
    const userId = this.userContext.selectedUser()?.id;
    if (!userId) return;
    const ref = this.dialog.open(PaymentSourceFormDialogComponent, {
      width: '450px',
      data: { users: this.users(), preselectedUserId: userId }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.paymentSourceService.create(result).subscribe({
          next: () => {
            this.snackBar.open('Payment source created', 'Close', { duration: 2000 });
            this.loadSources(userId);
          },
          error: () => this.snackBar.open('Failed to create payment source', 'Close', { duration: 3000 })
        });
      }
    });
  }

  openEditDialog(ps: PaymentSource): void {
    const userId = this.userContext.selectedUser()?.id;
    if (!userId) return;
    const ref = this.dialog.open(PaymentSourceFormDialogComponent, {
      width: '450px',
      data: { paymentSource: ps, users: this.users() }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.paymentSourceService.update(ps.id, result).subscribe({
          next: () => {
            this.snackBar.open('Payment source updated', 'Close', { duration: 2000 });
            this.loadSources(userId);
          },
          error: () => this.snackBar.open('Failed to update payment source', 'Close', { duration: 3000 })
        });
      }
    });
  }

  deletePaymentSource(ps: PaymentSource): void {
    const userId = this.userContext.selectedUser()?.id;
    if (!userId) return;
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payment Source', message: `Are you sure you want to delete "${ps.name}"?` }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.paymentSourceService.delete(ps.id).subscribe({
          next: () => {
            this.snackBar.open('Payment source deleted', 'Close', { duration: 2000 });
            this.loadSources(userId);
          },
          error: () => this.snackBar.open('Failed to delete payment source', 'Close', { duration: 3000 })
        });
      }
    });
  }
}
