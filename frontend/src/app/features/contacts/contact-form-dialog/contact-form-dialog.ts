import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Contact } from '../../../core/models/contact.model';

@Component({
  selector: 'app-contact-form-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './contact-form-dialog.html'
})
export class ContactFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ContactFormDialogComponent>);
  readonly data = inject<{ contact?: Contact }>(MAT_DIALOG_DATA);

  readonly form = new FormGroup({
    name: new FormControl(this.data?.contact?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
