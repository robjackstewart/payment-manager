import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { describe, it, expect, beforeEach } from 'vitest';
import { ConfirmDialogComponent } from './confirm-dialog';

describe('ConfirmDialogComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { title: 'Confirm Delete', message: 'Are you sure?' } }
      ]
    });
  });

  it('should initialise with title and message from MAT_DIALOG_DATA', () => {
    const fixture = TestBed.createComponent(ConfirmDialogComponent);
    const component = fixture.componentInstance;

    expect(component.data.title).toBe('Confirm Delete');
    expect(component.data.message).toBe('Are you sure?');
  });
});
