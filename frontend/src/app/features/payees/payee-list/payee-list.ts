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
import { PayeeService } from '../../../core/services/payee.service';
import { UserService } from '../../../core/services/user.service';
import { UserContextService } from '../../../core/services/user-context.service';
import { Payee } from '../../../core/models/payee.model';
import { User } from '../../../core/models/user.model';
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
    MatSelectModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './payee-list.html',
  styleUrl: './payee-list.scss'
})
export class PayeeListComponent implements OnInit {
  private readonly payeeService = inject(PayeeService);
  private readonly userService = inject(UserService);
  readonly userContext = inject(UserContextService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly payees = signal<Payee[]>([]);
  readonly users = signal<User[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'actions'];

  constructor() {
    effect(() => {
      const user = this.userContext.selectedUser();
      if (user) this.loadPayees(user.id);
      else this.payees.set([]);
    });
  }

  ngOnInit(): void {
    this.userService.getAll().subscribe(users => this.users.set(users));
  }

  private loadPayees(userId: string): void {
    this.loading.set(true);
    this.payeeService.getAll(userId).subscribe({
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
    const userId = this.userContext.selectedUser()?.id;
    if (!userId) return;
    const ref = this.dialog.open(PayeeFormDialogComponent, {
      width: '450px',
      data: { users: this.users(), preselectedUserId: userId }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.payeeService.create(result).subscribe({
          next: () => {
            this.snackBar.open('Payee created', 'Close', { duration: 2000 });
            this.loadPayees(userId);
          },
          error: () => this.snackBar.open('Failed to create payee', 'Close', { duration: 3000 })
        });
      }
    });
  }

  openEditDialog(payee: Payee): void {
    const userId = this.userContext.selectedUser()?.id;
    if (!userId) return;
    const ref = this.dialog.open(PayeeFormDialogComponent, {
      width: '450px',
      data: { payee, users: this.users() }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.payeeService.update(payee.id, result).subscribe({
          next: () => {
            this.snackBar.open('Payee updated', 'Close', { duration: 2000 });
            this.loadPayees(userId);
          },
          error: () => this.snackBar.open('Failed to update payee', 'Close', { duration: 3000 })
        });
      }
    });
  }

  deletePayee(payee: Payee): void {
    const userId = this.userContext.selectedUser()?.id;
    if (!userId) return;
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payee', message: `Are you sure you want to delete "${payee.name}"?` }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.payeeService.delete(payee.id).subscribe({
          next: () => {
            this.snackBar.open('Payee deleted', 'Close', { duration: 2000 });
            this.loadPayees(userId);
          },
          error: () => this.snackBar.open('Failed to delete payee', 'Close', { duration: 3000 })
        });
      }
    });
  }
}
