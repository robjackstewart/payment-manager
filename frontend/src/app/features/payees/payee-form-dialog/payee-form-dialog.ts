import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Payee } from '../../../core/models/payee.model';

@Component({
  selector: 'app-payee-form-dialog',
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
  templateUrl: './payee-form-dialog.html'
})
export class PayeeFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PayeeFormDialogComponent>);
  readonly data = inject<{ payee?: Payee }>(MAT_DIALOG_DATA);

  readonly form = new FormGroup({
    name: new FormControl(this.data?.payee?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
