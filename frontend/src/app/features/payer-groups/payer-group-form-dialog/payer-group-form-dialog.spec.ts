import { describe, it, expect, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { PayerGroupFormDialogComponent } from './payer-group-form-dialog';

const mockPerson = { id: 'p1', userId: 'u1', name: 'Alice' };

async function setup(data: { payerGroup?: { id: string; name: string; memberPersonIds?: string[] }; people?: typeof mockPerson[] } = {}) {
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [PayerGroupFormDialogComponent],
    providers: [
      { provide: MatDialogRef, useValue: { close: vi.fn() } },
      { provide: MAT_DIALOG_DATA, useValue: data }
    ]
  }).compileComponents();
  const fixture = TestBed.createComponent(PayerGroupFormDialogComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance };
}

describe('PayerGroupFormDialogComponent', () => {
  describe('create mode (no payerGroup in data)', () => {
    it('has title "New Payer Group"', async () => {
      const { component } = await setup();
      expect(component.title).toBe('New Payer Group');
    });

    it('has submitLabel "Create"', async () => {
      const { component } = await setup();
      expect(component.submitLabel).toBe('Create');
    });

    it('initialises name control to empty string', async () => {
      const { component } = await setup();
      expect(component.form.controls.name.value).toBe('');
    });

    it('initialises personIds control to empty array', async () => {
      const { component } = await setup();
      expect(component.form.controls.personIds.value).toEqual([]);
    });
  });

  describe('edit mode (payerGroup provided in data)', () => {
    const payerGroup = { id: '1', name: 'Family', memberPersonIds: ['p1'] };

    it('has title "Edit Payer Group"', async () => {
      const { component } = await setup({ payerGroup });
      expect(component.title).toBe('Edit Payer Group');
    });

    it('has submitLabel "Save"', async () => {
      const { component } = await setup({ payerGroup });
      expect(component.submitLabel).toBe('Save');
    });

    it('pre-fills name control with the payer group name', async () => {
      const { component } = await setup({ payerGroup });
      expect(component.form.controls.name.value).toBe(payerGroup.name);
    });

    it('pre-fills personIds control with the payer group members', async () => {
      const { component } = await setup({ payerGroup });
      expect(component.form.controls.personIds.value).toEqual(['p1']);
    });
  });

  describe('people', () => {
    it('exposes the people passed in via dialog data', async () => {
      const { component } = await setup({ people: [mockPerson] });
      expect(component.people).toEqual([mockPerson]);
    });

    it('defaults to an empty array when none are provided', async () => {
      const { component } = await setup();
      expect(component.people).toEqual([]);
    });
  });

  describe('submit()', () => {
    it('closes the dialog with form value when the form is valid', async () => {
      const { component } = await setup();
      component.form.controls.name.setValue('Family');
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).toHaveBeenCalledWith({ name: 'Family', personIds: [] });
    });

    it('closes the dialog with the selected members', async () => {
      const { component } = await setup();
      component.form.controls.name.setValue('Family');
      component.form.controls.personIds.setValue(['p1']);
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).toHaveBeenCalledWith({ name: 'Family', personIds: ['p1'] });
    });

    it('does not close the dialog when the form is invalid (empty name)', async () => {
      const { component } = await setup();
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).not.toHaveBeenCalled();
    });
  });
});
