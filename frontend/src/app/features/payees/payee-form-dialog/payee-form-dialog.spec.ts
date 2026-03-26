import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { describe, it, expect, vi } from 'vitest';
import { PayeeFormDialogComponent } from './payee-form-dialog';
import { Payee } from '../../../core/models/payee.model';

function setup(data: { payee?: Payee } = {}) {
  TestBed.resetTestingModule();
  const dialogRef = { close: vi.fn() } as unknown as MatDialogRef<PayeeFormDialogComponent>;

  TestBed.configureTestingModule({
    imports: [PayeeFormDialogComponent],
    providers: [
      { provide: MatDialogRef, useValue: dialogRef },
      { provide: MAT_DIALOG_DATA, useValue: data },
    ],
  });

  const fixture = TestBed.createComponent(PayeeFormDialogComponent);
  fixture.detectChanges();
  return { component: fixture.componentInstance, dialogRef };
}

describe('PayeeFormDialogComponent', () => {
  describe('create mode (no payee in data)', () => {
    it('has the correct title', () => {
      const { component } = setup();
      expect(component.title).toBe('New Payee');
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

  describe('edit mode (payee provided)', () => {
    const payee: Payee = { id: '1', userId: 'u1', name: 'Alice' };

    it('has the correct title', () => {
      const { component } = setup({ payee });
      expect(component.title).toBe('Edit Payee');
    });

    it('has the correct submitLabel', () => {
      const { component } = setup({ payee });
      expect(component.submitLabel).toBe('Save');
    });

    it('pre-fills the name control with the existing value', () => {
      const { component } = setup({ payee });
      expect(component.form.controls.name.value).toBe('Alice');
    });
  });

  describe('submit()', () => {
    it('calls dialogRef.close with the form value when the form is valid', () => {
      const { component, dialogRef } = setup();
      component.form.controls.name.setValue('Alice');

      component.submit();

      expect(dialogRef.close).toHaveBeenCalledWith({ name: 'Alice' });
    });

    it('does not call dialogRef.close when the form is invalid', () => {
      const { component, dialogRef } = setup();
      component.form.controls.name.setValue('');

      component.submit();

      expect(dialogRef.close).not.toHaveBeenCalled();
    });
  });
});
