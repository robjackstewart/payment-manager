import { describe, it, expect, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { PersonFormDialogComponent } from './person-form-dialog';

async function setup(data: { person?: { id: string; name: string } } = {}) {
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [PersonFormDialogComponent],
    providers: [
      { provide: MatDialogRef, useValue: { close: vi.fn() } },
      { provide: MAT_DIALOG_DATA, useValue: data }
    ]
  }).compileComponents();
  const fixture = TestBed.createComponent(PersonFormDialogComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance };
}

describe('PersonFormDialogComponent', () => {
  describe('create mode (no person in data)', () => {
    it('has title "New Person"', async () => {
      const { component } = await setup();
      expect(component.title).toBe('New Person');
    });

    it('has submitLabel "Create"', async () => {
      const { component } = await setup();
      expect(component.submitLabel).toBe('Create');
    });

    it('initialises name control to empty string', async () => {
      const { component } = await setup();
      expect(component.form.controls.name.value).toBe('');
    });
  });

  describe('edit mode (person provided in data)', () => {
    const person = { id: '1', name: 'Bob' };

    it('has title "Edit Person"', async () => {
      const { component } = await setup({ person });
      expect(component.title).toBe('Edit Person');
    });

    it('has submitLabel "Save"', async () => {
      const { component } = await setup({ person });
      expect(component.submitLabel).toBe('Save');
    });

    it('pre-fills name control with the person name', async () => {
      const { component } = await setup({ person });
      expect(component.form.controls.name.value).toBe(person.name);
    });
  });

  describe('submit()', () => {
    it('closes the dialog with form value when the form is valid', async () => {
      const { component } = await setup();
      component.form.controls.name.setValue('Alice');
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).toHaveBeenCalledWith({ name: 'Alice' });
    });

    it('does not close the dialog when the form is invalid (empty name)', async () => {
      const { component } = await setup();
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).not.toHaveBeenCalled();
    });
  });
});
