import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Person } from '../../../core/models/person.model';

@Component({
  selector: 'app-person-form-dialog',
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
  templateUrl: './person-form-dialog.html'
})
export class PersonFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PersonFormDialogComponent>);
  readonly data = inject<{ person?: Person }>(MAT_DIALOG_DATA);

  readonly title = this.data?.person ? 'Edit Person' : 'New Person';
  readonly submitLabel = this.data?.person ? 'Save' : 'Create';

  readonly form = new FormGroup({
    name: new FormControl(this.data?.person?.name ?? '', [Validators.required]),
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
