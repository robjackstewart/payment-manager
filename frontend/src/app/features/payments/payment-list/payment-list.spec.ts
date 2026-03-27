import { describe, it, expect, vi } from 'vitest';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PaymentListComponent } from './payment-list';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentSourceService } from '../../../core/services/payment-source.service';
import { PayeeService } from '../../../core/services/payee.service';
import { ContactService } from '../../../core/services/contact.service';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { Payment } from '../../../core/models/payment.model';
import { PaymentFrequency } from '../../../core/models/payment-frequency.enum';

const mockPayee = { id: 'py1', name: 'Alice' };
const mockPaymentSource = { id: 'ps1', name: 'Bank' };
const mockContact = { id: 'c1', name: 'Bob' };

const mockPayment: Payment = {
  id: 'p1',
  userId: 'u1',
  paymentSourceId: 'ps1',
  payeeId: 'py1',
  currentAmount: 100,
  initialAmount: 100,
  values: [],
  currency: 'USD',
  frequency: PaymentFrequency.Monthly,
  startDate: '2024-01-01',
  userShare: { percentage: 50, value: 50 },
  splits: [],
  description: 'Rent',
};

function setup(isMobile = false, paymentOverride?: Partial<Payment>) {
  TestBed.resetTestingModule();
  const payment = paymentOverride ? { ...mockPayment, ...paymentOverride } : mockPayment;
  const paymentService = {
    getAll: vi.fn().mockReturnValue(of([payment])),
    create: vi.fn().mockReturnValue(of({})),
    update: vi.fn().mockReturnValue(of({})),
    delete: vi.fn().mockReturnValue(of(undefined)),
    addValue: vi.fn().mockReturnValue(of({})),
    removeValue: vi.fn().mockReturnValue(of(undefined)),
  };
  const paymentSourceService = { getAll: vi.fn().mockReturnValue(of([mockPaymentSource])) };
  const payeeService = { getAll: vi.fn().mockReturnValue(of([mockPayee])) };
  const contactService = { getAll: vi.fn().mockReturnValue(of([mockContact])) };
  const dialogRef = { afterClosed: vi.fn().mockReturnValue(of(null)) };
  const dialog = { open: vi.fn().mockReturnValue(dialogRef) };
  const snackBar = { open: vi.fn() };
  const isMobileSignal = signal(isMobile);
  const breakpointService = { isMobile: isMobileSignal };

  TestBed.configureTestingModule({
    imports: [PaymentListComponent],
    providers: [
      { provide: PaymentService, useValue: paymentService },
      { provide: PaymentSourceService, useValue: paymentSourceService },
      { provide: PayeeService, useValue: payeeService },
      { provide: ContactService, useValue: contactService },
      { provide: MatDialog, useValue: dialog },
      { provide: MatSnackBar, useValue: snackBar },
      { provide: BreakpointService, useValue: breakpointService },
    ],
  });

  const fixture = TestBed.createComponent(PaymentListComponent);
  return { fixture, component: fixture.componentInstance, paymentService, dialogRef, dialog, snackBar, isMobileSignal };
}

describe('PaymentListComponent', () => {
  describe('paymentsViewModel', () => {
    async function resolvedViewModel(paymentOverride?: Partial<Payment>) {
      const { fixture, component } = setup(false, paymentOverride);
      fixture.detectChanges();
      await fixture.whenStable();
      return component.paymentsViewModel();
    }

    it('resolves payeeName from the payees map', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].payeeName).toBe('Alice');
    });

    it('falls back to payeeId when payee is not found in the map', async () => {
      const vm = await resolvedViewModel({ payeeId: 'unknown-id' });
      expect(vm[0].payeeName).toBe('unknown-id');
    });

    it('formats amount as a currency string', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].formattedAmount).toMatch(/\$100/);
    });

    it('renders integer percentage without decimal places', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].yourShareDisplay).toBe('50%');
    });

    it('renders fractional percentage with two decimal places', async () => {
      const vm = await resolvedViewModel({ userShare: { percentage: 33.33, value: 33.33 } });
      expect(vm[0].yourShareDisplay).toBe('33.33%');
    });

    it('shows the frequency label from PAYMENT_FREQUENCY_LABELS', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].frequencyLabel).toBe('Monthly');
    });

    it('formats startDate as a medium date string', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].formattedStartDate).toMatch(/Jan/);
      expect(vm[0].formattedStartDate).toMatch(/2024/);
    });

    it('formats endDate as a medium date string when present', async () => {
      const vm = await resolvedViewModel({ endDate: '2024-12-31' });
      expect(vm[0].formattedEndDate).toMatch(/Dec/);
      expect(vm[0].formattedEndDate).toMatch(/2024/);
    });

    it('shows em dash for endDate when absent', async () => {
      const vm = await resolvedViewModel({ endDate: undefined });
      expect(vm[0].formattedEndDate).toBe('—');
    });

    it('uses description as descriptionDisplay when present', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0].descriptionDisplay).toBe('Rent');
    });

    it('uses em dash as descriptionDisplay when description is empty', async () => {
      const vm = await resolvedViewModel({ description: '' });
      expect(vm[0].descriptionDisplay).toBe('—');
    });

    it('preserves the original payment on _raw', async () => {
      const vm = await resolvedViewModel();
      expect(vm[0]._raw.id).toBe('p1');
    });
  });

  describe('openCreateDialog()', () => {
    const createRequest = {
      paymentSourceId: 'ps1', payeeId: 'py1', amount: 100,
      currency: 'USD', frequency: PaymentFrequency.Monthly, startDate: '2024-01-01',
    };

    it('calls paymentService.create and shows success snackbar', async () => {
      const { fixture, component, paymentService, dialogRef, snackBar } = setup();
      dialogRef.afterClosed.mockReturnValue(of(createRequest));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(paymentService.create).toHaveBeenCalledWith(createRequest);
      expect(snackBar.open).toHaveBeenCalledWith('Payment created', 'Close', { duration: 2000 });
    });

    it('does not call create when dialog is cancelled', async () => {
      const { fixture, component, paymentService, dialogRef } = setup();
      dialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(paymentService.create).not.toHaveBeenCalled();
    });

    it('shows error snackbar when create fails', async () => {
      const { fixture, component, paymentService, dialogRef, snackBar } = setup();
      dialogRef.afterClosed.mockReturnValue(of(createRequest));
      paymentService.create.mockReturnValue(throwError(() => new Error('fail')));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openCreateDialog();

      expect(snackBar.open).toHaveBeenCalledWith('Failed to create payment', 'Close', { duration: 3000 });
    });
  });

  describe('openEditDialog()', () => {
    const metadataRequest = {
      paymentSourceId: 'ps1', payeeId: 'py1', initialAmount: 100,
      currency: 'USD', frequency: PaymentFrequency.Monthly, startDate: '2024-01-01',
    };

    it('calls paymentService.update and shows success snackbar', async () => {
      const { fixture, component, paymentService, dialogRef, snackBar } = setup();
      dialogRef.afterClosed.mockReturnValue(of({ metadataRequest, valuesToUpsert: [], valuesToRemove: [] }));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPayment);

      expect(paymentService.update).toHaveBeenCalledWith(mockPayment.id, metadataRequest);
      expect(snackBar.open).toHaveBeenCalledWith('Payment updated', 'Close', { duration: 2000 });
    });

    it('does not call update when dialog is cancelled', async () => {
      const { fixture, component, paymentService, dialogRef } = setup();
      dialogRef.afterClosed.mockReturnValue(of(null));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPayment);

      expect(paymentService.update).not.toHaveBeenCalled();
    });

    it('shows error snackbar when update fails', async () => {
      const { fixture, component, paymentService, dialogRef, snackBar } = setup();
      dialogRef.afterClosed.mockReturnValue(of({ metadataRequest, valuesToUpsert: [], valuesToRemove: [] }));
      paymentService.update.mockReturnValue(throwError(() => new Error('fail')));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.openEditDialog(mockPayment);

      expect(snackBar.open).toHaveBeenCalledWith('Failed to update payment', 'Close', { duration: 3000 });
    });
  });

  describe('deletePayment()', () => {
    it('calls delete and shows success snackbar when confirmed', async () => {
      const { fixture, component, paymentService, dialogRef, snackBar } = setup();
      dialogRef.afterClosed.mockReturnValue(of(true));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePayment(mockPayment);

      expect(paymentService.delete).toHaveBeenCalledWith(mockPayment.id);
      expect(snackBar.open).toHaveBeenCalledWith('Payment deleted', 'Close', { duration: 2000 });
    });

    it('does not call delete when cancelled', async () => {
      const { fixture, component, paymentService, dialogRef } = setup();
      dialogRef.afterClosed.mockReturnValue(of(false));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePayment(mockPayment);

      expect(paymentService.delete).not.toHaveBeenCalled();
    });

    it('shows error snackbar when delete fails', async () => {
      const { fixture, component, paymentService, dialogRef, snackBar } = setup();
      dialogRef.afterClosed.mockReturnValue(of(true));
      paymentService.delete.mockReturnValue(throwError(() => new Error('fail')));
      fixture.detectChanges();
      await fixture.whenStable();

      await component.deletePayment(mockPayment);

      expect(snackBar.open).toHaveBeenCalledWith('Failed to delete payment', 'Close', { duration: 3000 });
    });
  });

  describe('responsive rendering', () => {
    it('on desktop renders a table and no mobile cards', async () => {
      const { fixture } = setup(false);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).not.toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).toBeNull();
    });

    it('on mobile renders a card list and no table', async () => {
      const { fixture } = setup(true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const nativeEl: HTMLElement = fixture.nativeElement;
      expect(nativeEl.querySelector('table[mat-table]')).toBeNull();
      expect(nativeEl.querySelector('.mobile-card-list')).not.toBeNull();
    });

    it('on mobile renders one card per payment', async () => {
      const { fixture } = setup(true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const cards = fixture.nativeElement.querySelectorAll('.mobile-card');
      expect(cards.length).toBe(1);
    });

    it('on mobile each card shows the payee name as the card title', async () => {
      const { fixture } = setup(true);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const titles = Array.from(fixture.nativeElement.querySelectorAll('.card-title') as NodeListOf<HTMLElement>)
        .map(el => el.textContent?.trim());
      expect(titles).toContain('Alice');
    });
  });
});
