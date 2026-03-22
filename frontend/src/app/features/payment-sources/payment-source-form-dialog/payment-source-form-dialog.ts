import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PaymentSource } from '../../../core/models/payment-source.model';

@Component({
  selector: 'app-payment-source-form-dialog',
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
  templateUrl: './payment-source-form-dialog.html'
})
export class PaymentSourceFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PaymentSourceFormDialogComponent>);
  readonly data = inject<{ paymentSource?: PaymentSource }>(MAT_DIALOG_DATA);

  readonly title = this.data?.paymentSource ? 'Edit Payment Source' : 'New Payment Source';
  readonly submitLabel = this.data?.paymentSource ? 'Save' : 'Create';

  readonly form = new FormGroup({
    name: new FormControl(this.data?.paymentSource?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
