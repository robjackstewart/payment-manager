import { Component, inject, OnInit, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ContactService } from '../../../core/services/contact.service';
import { Contact } from '../../../core/models/contact.model';
import { ContactFormDialogComponent } from '../contact-form-dialog/contact-form-dialog';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-contact-list',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './contact-list.html',
  styleUrl: './contact-list.scss'
})
export class ContactListComponent implements OnInit {
  private readonly contactService = inject(ContactService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly contacts = signal<Contact[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.contactService.getAll().subscribe({
      next: contacts => {
        this.contacts.set(contacts);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load contacts', 'Close', { duration: 3000 });
      }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(ContactFormDialogComponent, { width: '450px', data: {} });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.contactService.create(result).subscribe({
          next: () => {
            this.snackBar.open('Contact created', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to create contact', 'Close', { duration: 3000 })
        });
      }
    });
  }

  openEditDialog(contact: Contact): void {
    const ref = this.dialog.open(ContactFormDialogComponent, {
      width: '450px',
      data: { contact }
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.contactService.update(contact.id, result).subscribe({
          next: () => {
            this.snackBar.open('Contact updated', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to update contact', 'Close', { duration: 3000 })
        });
      }
    });
  }

  deleteContact(contact: Contact): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Contact', message: `Are you sure you want to delete "${contact.name}"?` }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.contactService.delete(contact.id).subscribe({
          next: () => {
            this.snackBar.open('Contact deleted', 'Close', { duration: 2000 });
            this.load();
          },
          error: () => this.snackBar.open('Failed to delete contact', 'Close', { duration: 3000 })
        });
      }
    });
  }
}
