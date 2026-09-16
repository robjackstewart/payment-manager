import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { vi, describe, it, expect } from 'vitest';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PersonService } from '../../../core/services/person.service';
import { PayerGroupService } from '../../../core/services/payer-group.service';
import { PersonListComponent } from './person-list';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { Person } from '../../../core/models/person.model';

const mockPayerGroup = { id: 'g1', userId: 'u1', name: 'Family', memberPersonIds: ['1'] };
const mockPerson: Person = { id: '1', userId: 'u1', name: 'Alice' };
const mockPerson2: Person = { id: '2', userId: 'u1', name: 'Bob' };

function setup(people: Person[] = [mockPerson], isMobile = false, payerGroups = [mockPayerGroup]) {
  TestBed.resetTestingModule();
  const mockPersonService = {
    getAll: vi.fn().mockReturnValue(of(people)),
    create: vi.fn().mockReturnValue(of({})),
    update: vi.fn().mockReturnValue(of({})),
    delete: vi.fn().mockReturnValue(of(undefined)),
  };
  const mockPayerGroupService = { getAll: vi.fn().mockReturnValue(of(payerGroups)) };
  const mockDialogRef = { afterClosed: vi.fn().mockReturnValue(of(null)) };
  const mockDialog = { open: vi.fn().mockReturnValue(mockDialogRef) };
  const mockSnackBar = { open: vi.fn() };
  const isMobileSignal = signal(isMobile);
  const breakpointService = { isMobile: isMobileSignal };

  TestBed.configureTestingModule({
    imports: [PersonListComponent],
    providers: [
      { provide: PersonService, useValue: mockPersonService },
      { provide: PayerGroupService, useValue: mockPayerGroupService },
      { provide: MatDialog, useValue: mockDialog },
      { provide: MatSnackBar, useValue: mockSnackBar },
      { provide: BreakpointService, useValue: breakpointService },
    ],
  });
  const fixture = TestBed.createComponent(PersonListComponent);
  return { fixture, component: fixture.componentInstance, mockPersonService, mockPayerGroupService, mockDialog, mockDialogRef, mockSnackBar, isMobileSignal };
}

describe('PersonListComponent', () => {
  describe('data loading', () => {
    it('returns people after loading', async () => {
      const { fixture, component } = setup([mockPerson]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.peopleForTable().map(vm => vm._raw)).toEqual([mockPerson]);
    });

    it('isLoading is false after loading', async () => {
      const { fixture, component } = setup([mockPerson]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.isLoading()).toBe(false);
    });

    it('resolves groupNames from the payer groups the person belongs to', async () => {
      const { fixture, component } = setup([mockPerson]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.peopleForTable()[0].groupNames).toBe('Family');
    });

    it('shows "None" for a person in no payer group', async () => {
      const { fixture, component } = setup([mockPerson2], false, [mockPayerGroup]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.peopleForTable()[0].groupNames).toBe('None');
    });

    it('joins multiple group names with a comma', async () => {
      const secondGroup = { id: 'g2', userId: 'u1', name: 'Housemates', memberPersonIds: ['1'] };
      const { fixture, component } = setup([mockPerson], false, [mockPayerGroup, secondGroup]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.peopleForTable()[0].groupNames).toBe('Family, Housemates');
    });
  });

  describe('openCreateDialog()', () => {
    it('calls create and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockPersonService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New' }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockPersonService.create).toHaveBeenCalledWith({ name: 'New' });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Person created', 'Close', { duration: 2000 });
    });

    it('does not call create when dialog is cancelled', async () => {
      const { fixture, component, mockPersonService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockPersonService.create).not.toHaveBeenCalled();
    });

    it('shows failure snackbar when service throws', async () => {
      const { fixture, component, mockPersonService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New' }));
      mockPersonService.create.mockReturnValue(throwError(() => new Error('fail')));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockSnackBar.open).toHaveBeenCalledWith('Failed to create person', 'Close', { duration: 3000 });
    });
  });

  describe('openEditDialog()', () => {
    it('calls update and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockPersonService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'Updated' }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPerson);

      expect(mockPersonService.update).toHaveBeenCalledWith(mockPerson.id, { name: 'Updated' });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Person updated', 'Close', { duration: 2000 });
    });

    it('does not call update when dialog is cancelled', async () => {
      const { fixture, component, mockPersonService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPerson);

      expect(mockPersonService.update).not.toHaveBeenCalled();
    });
  });

  describe('deletePerson()', () => {
    it('calls delete and shows success snackbar when confirmed', async () => {
      const { fixture, component, mockPersonService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(true));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePerson(mockPerson);

      expect(mockPersonService.delete).toHaveBeenCalledWith(mockPerson.id);
      expect(mockSnackBar.open).toHaveBeenCalledWith('Person deleted', 'Close', { duration: 2000 });
    });

    it('does not call delete when cancelled', async () => {
      const { fixture, component, mockPersonService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(false));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePerson(mockPerson);

      expect(mockPersonService.delete).not.toHaveBeenCalled();
    });
  });

  describe('responsive rendering', () => {
    it('on desktop renders a table and no mobile cards', async () => {
      const { fixture } = setup([mockPerson, mockPerson2], false);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).not.toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).toBeNull();
    });

    it('on mobile renders cards and no table', async () => {
      const { fixture } = setup([mockPerson, mockPerson2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).not.toBeNull();
    });

    it('on mobile renders one card per person', async () => {
      const { fixture } = setup([mockPerson, mockPerson2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const cards = fixture.nativeElement.querySelectorAll('.mobile-card');
      expect(cards.length).toBe(2);
    });

    it('on mobile each card shows the person name', async () => {
      const { fixture } = setup([mockPerson, mockPerson2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const titles = Array.from(fixture.nativeElement.querySelectorAll('.card-title') as NodeListOf<HTMLElement>)
        .map(el => el.textContent?.trim());
      expect(titles?.some(t => t?.includes('Alice'))).toBe(true);
      expect(titles?.some(t => t?.includes('Bob'))).toBe(true);
    });

    it('on mobile renders a delete button for every person', async () => {
      const { fixture } = setup([mockPerson2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const card = fixture.nativeElement.querySelector('.mobile-card');
      expect(card.querySelector('button[color="warn"]')).not.toBeNull();
    });
  });
});
