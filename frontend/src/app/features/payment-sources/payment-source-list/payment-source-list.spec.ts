import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { PaymentSourceListComponent } from './payment-source-list';
import { PaymentSource } from '../../../core/models/payment-source.model';

const mockPaymentSource: PaymentSource = { id: '1', userId: 'u1', name: 'Main Account' };

function setup(paymentSources: PaymentSource[] = [mockPaymentSource]) {
  const mockPaymentSourceService = {
    getAll: vi.fn().mockReturnValue(of(paymentSources)),
    create: vi.fn().mockReturnValue(of({})),
    update: vi.fn().mockReturnValue(of({})),
    delete: vi.fn().mockReturnValue(of(undefined)),
  };
  const mockDialogRef = { afterClosed: vi.fn().mockReturnValue(of(null)) };
  const mockDialog = { open: vi.fn().mockReturnValue(mockDialogRef) };
  const mockSnackBar = { open: vi.fn() };

  TestBed.configureTestingModule({
    imports: [PaymentSourceListComponent],
    providers: [
      { provide: PaymentSourceService, useValue: mockPaymentSourceService },
      { provide: MatDialog, useValue: mockDialog },
      { provide: MatSnackBar, useValue: mockSnackBar },
    ],
  });
  const fixture = TestBed.createComponent(PaymentSourceListComponent);
  return { fixture, component: fixture.componentInstance, mockPaymentSourceService, mockDialog, mockDialogRef, mockSnackBar };
}

describe('PaymentSourceListComponent', () => {
  beforeEach(() => TestBed.resetTestingModule());

  describe('data loading', () => {
    it('returns payment sources after loading', async () => {
      const { fixture, component } = setup([mockPaymentSource]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.paymentSourcesForTable()).toEqual([mockPaymentSource]);
    });

    it('isLoading is false after loading', async () => {
      const { fixture, component } = setup([mockPaymentSource]);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(component.isLoading()).toBe(false);
    });
  });

  describe('openCreateDialog()', () => {
    it('calls create and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockPaymentSourceService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New Source' }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockPaymentSourceService.create).toHaveBeenCalledWith({ name: 'New Source' });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Payment source created', 'Close', { duration: 2000 });
    });

    it('does not call create when dialog is cancelled', async () => {
      const { fixture, component, mockPaymentSourceService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockPaymentSourceService.create).not.toHaveBeenCalled();
    });

    it('shows failure snackbar when service throws', async () => {
      const { fixture, component, mockPaymentSourceService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'New Source' }));
      mockPaymentSourceService.create.mockReturnValue(throwError(() => new Error('fail')));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(mockSnackBar.open).toHaveBeenCalledWith('Failed to create payment source', 'Close', { duration: 3000 });
    });
  });

  describe('openEditDialog()', () => {
    it('calls update and shows success snackbar when dialog returns a result', async () => {
      const { fixture, component, mockPaymentSourceService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of({ name: 'Updated Source' }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPaymentSource);

      expect(mockPaymentSourceService.update).toHaveBeenCalledWith(mockPaymentSource.id, { name: 'Updated Source' });
      expect(mockSnackBar.open).toHaveBeenCalledWith('Payment source updated', 'Close', { duration: 2000 });
    });

    it('does not call update when dialog is cancelled', async () => {
      const { fixture, component, mockPaymentSourceService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPaymentSource);

      expect(mockPaymentSourceService.update).not.toHaveBeenCalled();
    });
  });

  describe('deletePaymentSource()', () => {
    it('calls delete and shows success snackbar when confirmed', async () => {
      const { fixture, component, mockPaymentSourceService, mockDialogRef, mockSnackBar } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(true));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePaymentSource(mockPaymentSource);

      expect(mockPaymentSourceService.delete).toHaveBeenCalledWith(mockPaymentSource.id);
      expect(mockSnackBar.open).toHaveBeenCalledWith('Payment source deleted', 'Close', { duration: 2000 });
    });

    it('does not call delete when cancelled', async () => {
      const { fixture, component, mockPaymentSourceService, mockDialogRef } = setup();
      mockDialogRef.afterClosed.mockReturnValue(of(false));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePaymentSource(mockPaymentSource);

      expect(mockPaymentSourceService.delete).not.toHaveBeenCalled();
    });
  });
});
