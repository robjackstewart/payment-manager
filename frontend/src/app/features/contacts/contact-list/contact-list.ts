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
import { ContactService } from '../../../core/services/contact.service';
import { Contact } from '../../../core/models/contact.model';
import { LOADING, LoadingState, isLoaded } from '../../../core/utils/loading.utils';

@Component({
  selector: 'app-contact-list',
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
  templateUrl: './contact-list.html',
  styleUrl: './contact-list.scss'
})
export class ContactListComponent {
  private readonly contactService = inject(ContactService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  private readonly reloadTrigger = signal(0);

  private readonly contactsResource = rxResource({
    params: () => this.reloadTrigger(),
    stream: () => this.contactService.getAll()
  });

  readonly contacts = computed<LoadingState<Contact[]>>(() =>
    this.contactsResource.isLoading() ? LOADING : (this.contactsResource.value() ?? [])
  );
  readonly isLoading = computed(() => this.contacts() === LOADING);
  readonly contactsForTable = computed(() => isLoaded(this.contacts()) ? this.contacts() as Contact[] : []);

  readonly displayedColumns = ['name', 'actions'];

  private reload(): void { this.reloadTrigger.update(n => n + 1); }

  async openCreateDialog(): Promise<void> {
    const [{ ContactFormDialogComponent }] = await Promise.all([import('../contact-form-dialog/contact-form-dialog')]);
    const ref = this.dialog.open(ContactFormDialogComponent, { width: '450px', data: {} });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.contactService.create(result));
      this.snackBar.open('Contact created', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to create contact', 'Close', { duration: 3000 });
    }
  }

  async openEditDialog(contact: Contact): Promise<void> {
    const { ContactFormDialogComponent } = await import('../contact-form-dialog/contact-form-dialog');
    const ref = this.dialog.open(ContactFormDialogComponent, {
      width: '450px',
      data: { contact }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.contactService.update(contact.id, result));
      this.snackBar.open('Contact updated', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to update contact', 'Close', { duration: 3000 });
    }
  }

  async deleteContact(contact: Contact): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Contact', message: `Are you sure you want to delete "${contact.name}"?` }
    });
    const confirmed = await firstValueFrom(ref.afterClosed());
    if (!confirmed) return;
    try {
      await firstValueFrom(this.contactService.delete(contact.id));
      this.snackBar.open('Contact deleted', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to delete contact', 'Close', { duration: 3000 });
    }
  }
}
