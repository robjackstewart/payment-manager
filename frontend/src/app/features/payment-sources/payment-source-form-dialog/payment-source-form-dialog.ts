import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PaymentSource } from '../../../core/models/payment-source.model';

@Component({
  selector: 'app-payment-source-form-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './payment-source-form-dialog.html'
})
export class PaymentSourceFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PaymentSourceFormDialogComponent>);
  readonly data = inject<{ paymentSource?: PaymentSource }>(MAT_DIALOG_DATA);

  readonly form = new FormGroup({
    name: new FormControl(this.data?.paymentSource?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
