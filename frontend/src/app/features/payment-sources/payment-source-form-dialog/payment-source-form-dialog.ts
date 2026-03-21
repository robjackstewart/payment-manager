import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-payment-source-form-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './payment-source-form-dialog.html'
})
export class PaymentSourceFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PaymentSourceFormDialogComponent>);
  readonly data = inject<{ paymentSource?: PaymentSource; users: User[] }>(MAT_DIALOG_DATA);

  readonly form = new FormGroup({
    userId: new FormControl(this.data?.paymentSource?.userId ?? '', [Validators.required]),
    name: new FormControl(this.data?.paymentSource?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
