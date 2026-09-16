import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { vi, describe, it, expect } from 'vitest';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PayerGroupService } from '../../../core/services/payer-group.service';
import { PersonService } from '../../../core/services/person.service';
import { PayerGroupListComponent } from './payer-group-list';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { PayerGroup } from '../../../core/models/payer-group.model';
import { Person } from '../../../core/models/person.model';

const mockPerson: Person = { id: 'p1', userId: 'u1', name: 'Alice' };
const mockPayerGroup: PayerGroup = { id: '1', userId: 'u1', name: 'Family', memberPersonIds: ['p1'] };
const mockPayerGroup2: PayerGroup = { id: '2', userId: 'u1', name: 'Housemates', memberPersonIds: [] };

function setup(payerGroups: PayerGroup[] = [mockPayerGroup], isMobile = false, people: Person[] = [mockPerson]) {
  TestBed.resetTestingModule();
  const mockPayerGroupService = {
    getAll: vi.fn().mockReturnValue(of(payerGroups)),
    create: vi.fn().mockReturnValue(of({ id: 'new-id' })),
    update: vi.fn().mockReturnValue(of({})),
    delete: vi.fn().mockReturnValue(of(undefined)),
    setMembers: vi.fn().mockReturnValue(of({})),
  };
  const mockPersonService = { getAll: vi.fn().mockReturnValue(of(people)) };
  const mockDialogRef = { afterClosed: vi.fn().mockReturnValue(of(null)) };
  const mockDialog = { open: vi.fn().mockReturnValue(mockDialogRef) };
  const mockSnackBar = { open: vi.fn() };
  const isMobileSignal = signal(isMobile);
  const breakpointService = { isMobile: isMobileSignal };

  TestBed.configureTestingModule({
    imports: [PayerGroupListComponent],
    providers: [
      { provide: PayerGroupService, useValue: mockPayerGroupService },
      { provide: PersonService, useValue: mockPersonService },
      { provide: MatDialog, useValue: mockDialog },
      { provide: MatSnackBar, useValue: mockSnackBar },
      { provide: BreakpointService, useValue: breakpointService },
    ],
  });
  const fixture = TestBed.createComponent(PayerGroupListComponent);
  return { fixture, component: fixture.componentInstance, mockPayerGroupService, mockPersonService, mockDialog, mockDialogRef, mockSnackBar, isMobileSignal };
}

describe('PayerGroupListComponent', () => {
  describe('data loading', () => {
    it('returns payer groups after loading', async () => {
      const { fixture, component } = setup([mockPayerGroup]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.payerGroupsForTable().map(vm => vm._raw)).toEqual([mockPayerGroup]);
    });

    it('isLoading is false after loading', async () => {
      const { fixture, component } = setup([mockPayerGroup]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.isLoading()).toBe(false);
    });

    it('resolves memberNames from the people map', async () => {
      const { fixture, component } = setup([mockPayerGroup]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.payerGroupsForTable()[0].memberNames).toBe('Alice');
    });

    it('shows "None" for a payer group with no members', async () => {
      const { fixture, component } = setup([mockPayerGroup2]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.payerGroupsForTable()[0].memberNames).toBe('None');
    });
  });

  describe('openCreateDialog()', () => {
    it('calls create and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New', personIds: [] }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockPayerGroupService.create).toHaveBeenCalledWith({ name: 'New' });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Payer group created', 'Close', { duration: 2000 });
    });

    it('calls setMembers with the selected people when provided', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New', personIds: ['p1'] }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockPayerGroupService.setMembers).toHaveBeenCalledWith('new-id', { personIds: ['p1'] });
    });

    it('does not call create when dialog is cancelled', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockPayerGroupService.create).not.toHaveBeenCalled();
    });

    it('shows failure snackbar when service throws', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New', personIds: [] }));
      mockPayerGroupService.create.mockReturnValue(throwError(() => new Error('fail')));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockSnackBar.open).toHaveBeenCalledWith('Failed to create payer group', 'Close', { duration: 3000 });
    });
  });

  describe('openEditDialog()', () => {
    it('calls update and setMembers and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'Updated', personIds: ['p1'] }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPayerGroup);

      expect(mockPayerGroupService.update).toHaveBeenCalledWith(mockPayerGroup.id, { name: 'Updated' });
      expect(mockPayerGroupService.setMembers).toHaveBeenCalledWith(mockPayerGroup.id, { personIds: ['p1'] });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Payer group updated', 'Close', { duration: 2000 });
    });

    it('does not call update when dialog is cancelled', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPayerGroup);

      expect(mockPayerGroupService.update).not.toHaveBeenCalled();
    });
  });

  describe('deletePayerGroup()', () => {
    it('calls delete and shows success snackbar when confirmed', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(true));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePayerGroup(mockPayerGroup);

      expect(mockPayerGroupService.delete).toHaveBeenCalledWith(mockPayerGroup.id);
      expect(mockSnackBar.open).toHaveBeenCalledWith('Payer group deleted', 'Close', { duration: 2000 });
    });

    it('does not call delete when cancelled', async () => {
      const { fixture, component, mockPayerGroupService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(false));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePayerGroup(mockPayerGroup);

      expect(mockPayerGroupService.delete).not.toHaveBeenCalled();
    });
  });

  describe('responsive rendering', () => {
    it('on desktop renders a table and no mobile cards', async () => {
      const { fixture } = setup([mockPayerGroup, mockPayerGroup2], false);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).not.toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).toBeNull();
    });

    it('on mobile renders cards and no table', async () => {
      const { fixture } = setup([mockPayerGroup, mockPayerGroup2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).not.toBeNull();
    });

    it('on mobile renders one card per payer group', async () => {
      const { fixture } = setup([mockPayerGroup, mockPayerGroup2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const cards = fixture.nativeElement.querySelectorAll('.mobile-card');
      expect(cards.length).toBe(2);
    });

    it('on mobile each card shows the payer group name', async () => {
      const { fixture } = setup([mockPayerGroup, mockPayerGroup2], true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const titles = Array.from(fixture.nativeElement.querySelectorAll('.card-title') as NodeListOf<HTMLElement>)
        .map(el => el.textContent?.trim());
      expect(titles).toContain('Family');
      expect(titles).toContain('Housemates');
    });
  });
});
