import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { vi, describe, it, expect } from 'vitest';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ContactService } from '../../../core/services/contact.service';
import { ContactListComponent } from './contact-list';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { Contact } from '../../../core/models/contact.model';

const mockContact: Contact = { id: '1', userId: 'u1', name: 'Alice' };
const mockContact2: Contact = { id: '2', userId: 'u1', name: 'Bob' };

function setup(contacts: Contact[] = [mockContact], isMobile = false) {
  TestBed.resetTestingModule();
  const mockContactService = {
    getAll: vi.fn().mockReturnValue(of(contacts)),
    create: vi.fn().mockReturnValue(of({})),
    update: vi.fn().mockReturnValue(of({})),
    delete: vi.fn().mockReturnValue(of(undefined)),
  };
  const mockDialogRef = { afterClosed: vi.fn().mockReturnValue(of(null)) };
  const mockDialog = { open: vi.fn().mockReturnValue(mockDialogRef) };
  const mockSnackBar = { open: vi.fn() };
  const isMobileSignal = signal(isMobile);
  const breakpointService = { isMobile: isMobileSignal };

  TestBed.configureTestingModule({
    imports: [ContactListComponent],
    providers: [
      { provide: ContactService, useValue: mockContactService },
      { provide: MatDialog, useValue: mockDialog },
      { provide: MatSnackBar, useValue: mockSnackBar },
      { provide: BreakpointService, useValue: breakpointService },
    ],
  });
  const fixture = TestBed.createComponent(ContactListComponent);
  return { fixture, component: fixture.componentInstance, mockContactService, mockDialog, mockDialogRef, mockSnackBar, isMobileSignal };
}

describe('ContactListComponent', () => {
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

  describe('responsive rendering', () => {
    it('on desktop renders a table and no mobile cards', async () => {
      const { fixture } = setup([mockContact, mockContact2], false);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).not.toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).toBeNull();
    });

    it('on mobile renders cards and no table', async () => {
      const { fixture } = setup([mockContact, mockContact2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).not.toBeNull();
    });

    it('on mobile renders one card per contact', async () => {
      const { fixture } = setup([mockContact, mockContact2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const cards = fixture.nativeElement.querySelectorAll('.mobile-card');
      expect(cards.length).toBe(2);
    });

    it('on mobile each card shows the contact name', async () => {
      const { fixture } = setup([mockContact, mockContact2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const titles = Array.from(fixture.nativeElement.querySelectorAll<HTMLElement>('.card-title'))
        .map(el => el.textContent?.trim());
      expect(titles).toContain('Alice');
      expect(titles).toContain('Bob');
    });
  });
});
