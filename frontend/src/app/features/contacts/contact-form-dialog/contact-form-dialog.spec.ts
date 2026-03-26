import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ContactFormDialogComponent } from './contact-form-dialog';

async function createComponent(data: { contact?: { id: string; name: string } } = {}) {
  await TestBed.configureTestingModule({
    imports: [ContactFormDialogComponent],
    providers: [
      { provide: MatDialogRef, useValue: { close: vi.fn() } },
      { provide: MAT_DIALOG_DATA, useValue: data }
    ]
  }).compileComponents();
  const fixture = TestBed.createComponent(ContactFormDialogComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance };
}

describe('ContactFormDialogComponent', () => {
  beforeEach(() => TestBed.resetTestingModule());

  describe('create mode (no contact in data)', () => {
    it('has title "New Contact"', async () => {
      const { component } = await createComponent();
      expect(component.title).toBe('New Contact');
    });

    it('has submitLabel "Create"', async () => {
      const { component } = await createComponent();
      expect(component.submitLabel).toBe('Create');
    });

    it('initialises name control to empty string', async () => {
      const { component } = await createComponent();
      expect(component.form.controls.name.value).toBe('');
    });
  });

  describe('edit mode (contact provided in data)', () => {
    const contact = { id: '1', name: 'Bob' };

    it('has title "Edit Contact"', async () => {
      const { component } = await createComponent({ contact });
      expect(component.title).toBe('Edit Contact');
    });

    it('has submitLabel "Save"', async () => {
      const { component } = await createComponent({ contact });
      expect(component.submitLabel).toBe('Save');
    });

    it('pre-fills name control with the contact name', async () => {
      const { component } = await createComponent({ contact });
      expect(component.form.controls.name.value).toBe(contact.name);
    });
  });

  describe('submit()', () => {
    it('closes the dialog with form value when the form is valid', async () => {
      const { component } = await createComponent();
      component.form.controls.name.setValue('Alice');
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).toHaveBeenCalledWith({ name: 'Alice' });
    });

    it('does not close the dialog when the form is invalid (empty name)', async () => {
      const { component } = await createComponent();
      component.submit();
      const dialogRef = TestBed.inject(MatDialogRef);
      expect(dialogRef.close).not.toHaveBeenCalled();
    });
  });
});
