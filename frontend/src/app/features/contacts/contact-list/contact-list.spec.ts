import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ContactService } from '../../../core/services/contact.service';
import { ContactListComponent } from './contact-list';
import { Contact } from '../../../core/models/contact.model';

const mockContact: Contact = { id: '1', userId: 'u1', name: 'Alice' };

function setup(contacts: Contact[] = [mockContact]) {
  const mockContactService = {
    getAll: vi.fn().mockReturnValue(of(contacts)),
    create: vi.fn().mockReturnValue(of({})),
    update: vi.fn().mockReturnValue(of({})),
    delete: vi.fn().mockReturnValue(of(undefined)),
  };
  const mockDialogRef = { afterClosed: vi.fn().mockReturnValue(of(null)) };
  const mockDialog = { open: vi.fn().mockReturnValue(mockDialogRef) };
  const mockSnackBar = { open: vi.fn() };

  TestBed.configureTestingModule({
    imports: [ContactListComponent],
    providers: [
      { provide: ContactService, useValue: mockContactService },
      { provide: MatDialog, useValue: mockDialog },
      { provide: MatSnackBar, useValue: mockSnackBar },
    ],
  });
  const fixture = TestBed.createComponent(ContactListComponent);
  return { fixture, component: fixture.componentInstance, mockContactService, mockDialog, mockDialogRef, mockSnackBar };
}

describe('ContactListComponent', () => {
  beforeEach(() => TestBed.resetTestingModule());

  describe('data loading', () => {
    it('returns contacts after loading', async () => {
      const { fixture, component } = setup([mockContact]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.contactsForTable()).toEqual([mockContact]);
    });

    it('isLoading is false after loading', async () => {
      const { fixture, component } = setup([mockContact]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.isLoading()).toBe(false);
    });
  });

  describe('openCreateDialog()', () => {
    it('calls create and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockContactService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New' }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockContactService.create).toHaveBeenCalledWith({ name: 'New' });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Contact created', 'Close', { duration: 2000 });
    });

    it('does not call create when dialog is cancelled', async () => {
      const { fixture, component, mockContactService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockContactService.create).not.toHaveBeenCalled();
    });

    it('shows failure snackbar when service throws', async () => {
      const { fixture, component, mockContactService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New' }));
      mockContactService.create.mockReturnValue(throwError(() => new Error('fail')));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockSnackBar.open).toHaveBeenCalledWith('Failed to create contact', 'Close', { duration: 3000 });
    });
  });

  describe('openEditDialog()', () => {
    it('calls update and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockContactService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'Updated' }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockContact);

      expect(mockContactService.update).toHaveBeenCalledWith(mockContact.id, { name: 'Updated' });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Contact updated', 'Close', { duration: 2000 });
    });

    it('does not call update when dialog is cancelled', async () => {
      const { fixture, component, mockContactService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockContact);

      expect(mockContactService.update).not.toHaveBeenCalled();
    });
  });

  describe('deleteContact()', () => {
    it('calls delete and shows success snackbar when confirmed', async () => {
      const { fixture, component, mockContactService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(true));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deleteContact(mockContact);

      expect(mockContactService.delete).toHaveBeenCalledWith(mockContact.id);
      expect(mockSnackBar.open).toHaveBeenCalledWith('Contact deleted', 'Close', { duration: 2000 });
    });

    it('does not call delete when cancelled', async () => {
      const { fixture, component, mockContactService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(false));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deleteContact(mockContact);

      expect(mockContactService.delete).not.toHaveBeenCalled();
    });
  });
});
