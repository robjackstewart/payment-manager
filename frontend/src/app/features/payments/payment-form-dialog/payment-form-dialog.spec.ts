import { describe, it, expect, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { PaymentFormDialogComponent } from './payment-form-dialog';
import { PaymentFrequency } from '../../../core/models/payment-frequency.enum';
import { PaymentDirection } from '../../../core/models/payment-direction.enum';
import { Payment } from '../../../core/models/payment.model';

const mockPaymentSource = { id: 'ps1', userId: 'u1', name: 'Bank' };
const mockPayee = { id: 'py1', userId: 'u1', name: 'Landlord' };
const mockCurrentUser = { id: 'self', userId: 'u1', name: 'Current User' };
const mockAlice = { id: 'c1', userId: 'u1', name: 'Alice' };
const mockBob = { id: 'c2', userId: 'u1', name: 'Bob' };
const mockGroup = { id: 'g1', userId: 'u1', name: 'Family', memberPersonIds: ['self', 'c1'] };

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
  direction: PaymentDirection.Outgoing,
  startDate: '2024-01-01',
  payerGroupId: 'g1',
  splits: [
    { personId: 'self', percentage: 60 },
    { personId: 'c1', percentage: 40 },
  ],
  description: 'Rent',
};

async function setup(data: {
  payment?: Payment;
  direction?: PaymentDirection;
  paymentSources?: typeof mockPaymentSource[];
  payees?: typeof mockPayee[];
  people?: typeof mockCurrentUser[];
  payerGroups?: typeof mockGroup[];
} = {}) {
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [PaymentFormDialogComponent],
    providers: [
      { provide: MatDialogRef, useValue: { close: vi.fn() } },
      {
        provide: MAT_DIALOG_DATA,
        useValue: {
          paymentSources: [mockPaymentSource],
          payees: [mockPayee],
          people: [mockCurrentUser, mockAlice, mockBob],
          payerGroups: [mockGroup],
          ...data,
        },
      },
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(PaymentFormDialogComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance };
}

describe('PaymentFormDialogComponent', () => {
  describe('create mode (Outgoing, default)', () => {
    it('has title "New Payment"', async () => {
      const { component } = await setup();
      expect(component.title).toBe('New Payment');
    });

    it('labels the payee field "Paid To"', async () => {
      const { component } = await setup();
      expect(component.payeeLabel).toBe('Paid To');
    });

    it('labels the payment source field "Paid From"', async () => {
      const { component } = await setup();
      expect(component.paymentSourceLabel).toBe('Paid From');
    });

    it('starts with no splits and can be split across any person', async () => {
      const { component } = await setup();
      expect(component.splits.length).toBe(0);
      expect(component.splitPersonOptions()).toEqual([mockCurrentUser, mockAlice, mockBob]);
    });
  });

  describe('create mode (Incoming)', () => {
    it('has title "New Income"', async () => {
      const { component } = await setup({ direction: PaymentDirection.Incoming });
      expect(component.title).toBe('New Income');
    });

    it('labels the payee field "Paid By"', async () => {
      const { component } = await setup({ direction: PaymentDirection.Incoming });
      expect(component.payeeLabel).toBe('Paid By');
    });

    it('labels the payment source field "Paid Into"', async () => {
      const { component } = await setup({ direction: PaymentDirection.Incoming });
      expect(component.paymentSourceLabel).toBe('Paid Into');
    });

    it('never carries a payer group (income belongs to people)', async () => {
      const { component } = await setup({ direction: PaymentDirection.Incoming });
      expect(component.form.controls.payerGroupId.value).toBeNull();
      expect(component.splitPersonOptions()).toEqual([mockCurrentUser, mockAlice, mockBob]);
    });
  });

  describe('edit mode', () => {
    it('has title "Edit Payment"', async () => {
      const { component } = await setup({ payment: mockPayment });
      expect(component.title).toBe('Edit Payment');
    });

    it('pre-fills payerGroupId from the payment', async () => {
      const { component } = await setup({ payment: mockPayment });
      expect(component.form.controls.payerGroupId.value).toBe('g1');
    });

    it('pre-fills the splits array from the payment', async () => {
      const { component } = await setup({ payment: mockPayment });
      expect(component.splits.length).toBe(2);
      expect(component.splits.at(0).value).toEqual({ personId: 'self', percentage: 60 });
      expect(component.splits.at(1).value).toEqual({ personId: 'c1', percentage: 40 });
    });
  });

  describe('split validation', () => {
    it('requires the splits to total exactly 100%', async () => {
      const { component } = await setup({ payment: mockPayment });
      component.splits.at(1).patchValue({ percentage: 30 });
      expect(component.splitsTotalDisplay()).toBe('90%');
      expect(component.splitsSumValid()).toBe(false);
      expect(component.submitDisabled()).toBe(true);

      component.splits.at(1).patchValue({ percentage: 40 });
      expect(component.splitsSumValid()).toBe(true);
    });

    it('flags a person appearing more than once', async () => {
      const { component } = await setup({ payment: mockPayment });
      component.splits.at(1).patchValue({ personId: 'self' });
      expect(component.hasDuplicatePeople()).toBe(true);
      expect(component.submitDisabled()).toBe(true);
    });
  });

  describe('payer group selection', () => {
    it('clears splits when the payer group changes', async () => {
      const { component } = await setup({ payment: mockPayment });
      expect(component.splits.length).toBe(2);

      component.form.controls.payerGroupId.setValue(null);

      expect(component.splits.length).toBe(0);
    });

    it('filters split people to the chosen group\'s members', async () => {
      const { component } = await setup();
      component.form.controls.payerGroupId.setValue('g1');
      expect(component.splitPersonOptions()).toEqual([mockCurrentUser, mockAlice]);
    });

    it('does not clear splits on initial load in edit mode', async () => {
      const { component } = await setup({ payment: mockPayment });
      expect(component.splits.length).toBe(2);
    });
  });

  describe('submit() — create mode', () => {
    it('closes the dialog with the person splits included', async () => {
      const { component } = await setup({ direction: PaymentDirection.Outgoing });
      component.form.patchValue({
        paymentSourceId: 'ps1',
        payeeId: 'py1',
        currency: 'USD',
        frequency: PaymentFrequency.Monthly,
        startDate: new Date(2025, 0, 1),
        amount: 3200,
      });
      component.form.controls.payerGroupId.setValue('g1');
      component.addSplit();
      component.splits.at(0).patchValue({ personId: 'c1', percentage: 100 });

      component.submit();

      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).toHaveBeenCalledWith(
        expect.objectContaining({
          direction: PaymentDirection.Outgoing,
          payerGroupId: 'g1',
          amount: 3200,
          splits: [{ personId: 'c1', percentage: 100 }],
        })
      );
    });

    it('forces payerGroupId to null for income', async () => {
      const { component } = await setup({ direction: PaymentDirection.Incoming });
      component.form.patchValue({
        paymentSourceId: 'ps1',
        payeeId: 'py1',
        currency: 'USD',
        frequency: PaymentFrequency.Monthly,
        startDate: new Date(2025, 0, 1),
        amount: 3200,
      });
      component.addSplit();
      component.splits.at(0).patchValue({ personId: 'self', percentage: 100 });

      component.submit();

      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).toHaveBeenCalledWith(
        expect.objectContaining({
          direction: PaymentDirection.Incoming,
          payerGroupId: null,
          splits: [{ personId: 'self', percentage: 100 }],
        })
      );
    });

    it('does not close the dialog when required fields are missing', async () => {
      const { component } = await setup();
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('does not close the dialog when the splits do not total 100%', async () => {
      const { component } = await setup({ direction: PaymentDirection.Outgoing });
      component.form.patchValue({
        paymentSourceId: 'ps1',
        payeeId: 'py1',
        currency: 'USD',
        frequency: PaymentFrequency.Monthly,
        startDate: new Date(2025, 0, 1),
        amount: 3200,
      });
      component.form.controls.payerGroupId.setValue('g1');
      component.addSplit();
      component.splits.at(0).patchValue({ personId: 'c1', percentage: 40 });

      component.submit();

      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).not.toHaveBeenCalled();
    });
  });

  describe('submit() — edit mode', () => {
    it('wraps the payload in metadataRequest with direction and payerGroupId', async () => {
      const { component } = await setup({ payment: mockPayment });

      component.submit();

      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).toHaveBeenCalledWith(
        expect.objectContaining({
          metadataRequest: expect.objectContaining({ direction: PaymentDirection.Outgoing, payerGroupId: 'g1' }),
        })
      );
    });
  });

  describe('frequency — end date clearing', () => {
    it('clears endDate when frequency changes to Once', async () => {
      const { fixture, component } = await setup();
      component.form.controls.endDate.setValue(new Date(2025, 5, 1));
      component.form.controls.frequency.setValue(PaymentFrequency.Once);
      fixture.detectChanges();

      expect(component.form.controls.endDate.value).toBeNull();
    });
  });
});
