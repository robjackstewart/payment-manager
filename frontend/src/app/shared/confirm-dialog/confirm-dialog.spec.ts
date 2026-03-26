import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { describe, it, expect } from 'vitest';
import { ConfirmDialogComponent } from './confirm-dialog';

function setup() {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [ConfirmDialogComponent],
    providers: [
      { provide: MAT_DIALOG_DATA, useValue: { title: 'Confirm Delete', message: 'Are you sure?' } },
    ],
  });
  const fixture = TestBed.createComponent(ConfirmDialogComponent);
  return { fixture, component: fixture.componentInstance };
}

describe('ConfirmDialogComponent', () => {
  it('should initialise with title and message from MAT_DIALOG_DATA', () => {
    const { component } = setup();

    expect(component.data.title).toBe('Confirm Delete');
    expect(component.data.message).toBe('Are you sure?');
  });
});
