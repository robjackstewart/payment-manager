import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { describe, it, expect, vi } from 'vitest';
import { PaymentSourceFormDialogComponent } from './payment-source-form-dialog';

function setup(data: { paymentSource?: { id: string; name: string } } = {}) {
  TestBed.resetTestingModule();
  const dialogRef = { close: vi.fn() } as unknown as MatDialogRef<PaymentSourceFormDialogComponent>;

  TestBed.configureTestingModule({
    imports: [PaymentSourceFormDialogComponent],
    providers: [
      { provide: MatDialogRef, useValue: dialogRef },
      { provide: MAT_DIALOG_DATA, useValue: data },
    ],
  });

  const fixture = TestBed.createComponent(PaymentSourceFormDialogComponent);
  fixture.detectChanges();
  return { component: fixture.componentInstance, dialogRef };
}

describe('PaymentSourceFormDialogComponent', () => {
  describe('create mode (no paymentSource in data)', () => {
    it('has the correct title', () => {
      const { component } = setup();
      expect(component.title).toBe('New Payment Source');
    });

    it('has the correct submitLabel', () => {
      const { component } = setup();
      expect(component.submitLabel).toBe('Create');
    });

    it('initialises the name control as empty', () => {
      const { component } = setup();
      expect(component.form.controls.name.value).toBe('');
    });
  });

  describe('edit mode (paymentSource provided)', () => {
    const paymentSource = { id: '1', name: 'Main Account' };

    it('has the correct title', () => {
      const { component } = setup({ paymentSource });
      expect(component.title).toBe('Edit Payment Source');
    });

    it('has the correct submitLabel', () => {
      const { component } = setup({ paymentSource });
      expect(component.submitLabel).toBe('Save');
    });

    it('pre-fills the name control with the existing value', () => {
      const { component } = setup({ paymentSource });
      expect(component.form.controls.name.value).toBe('Main Account');
    });
  });

  describe('submit()', () => {
    it('calls dialogRef.close with the form value when the form is valid', () => {
      const { component, dialogRef } = setup();
      component.form.controls.name.setValue('Savings');

      component.submit();

      expect(dialogRef.close).toHaveBeenCalledWith({ name: 'Savings' });
    });

    it('does not call dialogRef.close when the form is invalid', () => {
      const { component, dialogRef } = setup();
      component.form.controls.name.setValue('');

      component.submit();

      expect(dialogRef.close).not.toHaveBeenCalled();
    });
  });
});
