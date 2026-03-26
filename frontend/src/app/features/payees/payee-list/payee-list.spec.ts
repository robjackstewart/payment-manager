import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PayeeListComponent } from './payee-list';
import { PayeeService } from '../../../core/services/payee.service';
import { Payee } from '../../../core/models/payee.model';

const mockPayees: Payee[] = [
  { id: '1', userId: 'u1', name: 'Alice' },
  { id: '2', userId: 'u1', name: 'Bob' },
];

function buildMockDialogRef(closeValue: unknown) {
  return { afterClosed: vi.fn().mockReturnValue(of(closeValue)) };
}

function setup() {
  const payeeService = {
    getAll: vi.fn().mockReturnValue(of(mockPayees)),
    create: vi.fn().mockReturnValue(of({ id: '3', userId: 'u1', name: 'Charlie' })),
    update: vi.fn().mockReturnValue(of({ id: '1', userId: 'u1', name: 'Alice Updated' })),
    delete: vi.fn().mockReturnValue(of(undefined)),
  };
  const dialog = { open: vi.fn() };
  const snackBar = { open: vi.fn() };

  TestBed.configureTestingModule({
    imports: [PayeeListComponent],
    providers: [
      { provide: PayeeService, useValue: payeeService },
      { provide: MatDialog, useValue: dialog },
      { provide: MatSnackBar, useValue: snackBar },
    ],
  });

  const fixture = TestBed.createComponent(PayeeListComponent);
  return { fixture, component: fixture.componentInstance, payeeService, dialog, snackBar };
}

describe('PayeeListComponent', () => {
  it('after init, payeesForTable() returns the loaded payees array', async () => {
    const { fixture, component } = setup();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(component.payeesForTable()).toEqual(mockPayees);
  });

  describe('openCreateDialog', () => {
    it('with a dialog result calls payeeService.create() and shows success snackbar', async () => {
      const { fixture, payeeService, dialog, snackBar } = setup();
      const mockRef = buildMockDialogRef({ name: 'Charlie' });
      dialog.open.mockReturnValue(mockRef);

      fixture.detectChanges();
      await fixture.whenStable();

      await fixture.componentInstance.openCreateDialog();

      expect(payeeService.create).toHaveBeenCalledWith({ name: 'Charlie' });
      expect(snackBar.open).toHaveBeenCalledWith('Payee created', 'Close', { duration: 2000 });
    });

    it('with null dialog result does NOT call payeeService.create()', async () => {
      const { fixture, payeeService, dialog } = setup();
      const mockRef = buildMockDialogRef(null);
      dialog.open.mockReturnValue(mockRef);

      fixture.detectChanges();
      await fixture.whenStable();

      await fixture.componentInstance.openCreateDialog();

      expect(payeeService.create).not.toHaveBeenCalled();
    });

    it('when payeeService.create() throws shows error snackbar', async () => {
      const { fixture, payeeService, dialog, snackBar } = setup();
      const mockRef = buildMockDialogRef({ name: 'Charlie' });
      dialog.open.mockReturnValue(mockRef);
      payeeService.create.mockReturnValue(throwError(() => new Error('network')));

      fixture.detectChanges();
      await fixture.whenStable();

      await fixture.componentInstance.openCreateDialog();

      expect(snackBar.open).toHaveBeenCalledWith('Failed to create payee', 'Close', { duration: 3000 });
    });
  });

  describe('openEditDialog', () => {
    it('with a dialog result calls payeeService.update() with the payee id and result', async () => {
      const { fixture, payeeService, dialog, snackBar } = setup();
      const payee = mockPayees[0];
      const mockRef = buildMockDialogRef({ name: 'Alice Updated' });
      dialog.open.mockReturnValue(mockRef);

      fixture.detectChanges();
      await fixture.whenStable();

      await fixture.componentInstance.openEditDialog(payee);

      expect(payeeService.update).toHaveBeenCalledWith(payee.id, { name: 'Alice Updated' });
      expect(snackBar.open).toHaveBeenCalledWith('Payee updated', 'Close', { duration: 2000 });
    });

    it('with null dialog result does NOT call payeeService.update()', async () => {
      const { fixture, payeeService, dialog } = setup();
      const payee = mockPayees[0];
      const mockRef = buildMockDialogRef(null);
      dialog.open.mockReturnValue(mockRef);

      fixture.detectChanges();
      await fixture.whenStable();

      await fixture.componentInstance.openEditDialog(payee);

      expect(payeeService.update).not.toHaveBeenCalled();
    });
  });

  describe('deletePayee', () => {
    it('when confirmed calls payeeService.delete() and shows success snackbar', async () => {
      const { fixture, payeeService, dialog, snackBar } = setup();
      const payee = mockPayees[0];
      const mockRef = buildMockDialogRef(true);
      dialog.open.mockReturnValue(mockRef);

      fixture.detectChanges();
      await fixture.whenStable();

      await fixture.componentInstance.deletePayee(payee);

      expect(payeeService.delete).toHaveBeenCalledWith(payee.id);
      expect(snackBar.open).toHaveBeenCalledWith('Payee deleted', 'Close', { duration: 2000 });
    });

    it('when cancelled does NOT call payeeService.delete()', async () => {
      const { fixture, payeeService, dialog } = setup();
      const payee = mockPayees[0];
      const mockRef = buildMockDialogRef(false);
      dialog.open.mockReturnValue(mockRef);

      fixture.detectChanges();
      await fixture.whenStable();

      await fixture.componentInstance.deletePayee(payee);

      expect(payeeService.delete).not.toHaveBeenCalled();
    });
  });
});
