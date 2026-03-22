import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Contact } from '../../../core/models/contact.model';

@Component({
  selector: 'app-contact-form-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatFormField,
    MatLabel,
    MatError,
    MatInput,
    ReactiveFormsModule,
  ],
  templateUrl: './contact-form-dialog.html'
})
export class ContactFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ContactFormDialogComponent>);
  readonly data = inject<{ contact?: Contact }>(MAT_DIALOG_DATA);

  readonly title = this.data?.contact ? 'Edit Contact' : 'New Contact';
  readonly submitLabel = this.data?.contact ? 'Save' : 'Create';

  readonly form = new FormGroup({
    name: new FormControl(this.data?.contact?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
