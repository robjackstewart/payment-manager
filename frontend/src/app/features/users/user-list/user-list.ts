import { Component, inject, OnInit, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UserService } from '../../../core/services/user.service';
import { User } from '../../../core/models/user.model';
import { UserFormDialogComponent } from '../user-form-dialog/user-form-dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss'
})
export class UserListComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly users = signal<User[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'actions'];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.userService.getAll().subscribe({
      next: users => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load users', 'Close', { duration: 3000 });
      }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(UserFormDialogComponent, { width: '400px', data: null });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.userService.create(result).subscribe({
          next: () => {
            this.snackBar.open('User created', 'Close', { duration: 2000 });
            this.loadUsers();
          },
          error: () => this.snackBar.open('Failed to create user', 'Close', { duration: 3000 })
        });
      }
    });
  }

  openEditDialog(user: User): void {
    const ref = this.dialog.open(UserFormDialogComponent, {
      width: '400px',
      data: { user }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.userService.update(user.id, result).subscribe({
          next: () => {
            this.snackBar.open('User updated', 'Close', { duration: 2000 });
            this.loadUsers();
          },
          error: () => this.snackBar.open('Failed to update user', 'Close', { duration: 3000 })
        });
      }
    });
  }

  deleteUser(user: User): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete User',
        message: `Are you sure you want to delete "${user.name}"?`
      }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.userService.delete(user.id).subscribe({
          next: () => {
            this.snackBar.open('User deleted', 'Close', { duration: 2000 });
            this.loadUsers();
          },
          error: () => this.snackBar.open('Failed to delete user', 'Close', { duration: 3000 })
        });
      }
    });
  }
}
