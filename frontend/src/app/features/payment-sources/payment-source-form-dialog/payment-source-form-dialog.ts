import { Component, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { FormField, FormRoot, form, required } from '@angular/forms/signals';
import { PaymentSource } from '../../../core/models/payment-source.model';

interface PaymentSourceFormModel {
  name: string;
}

@Component({
  selector: 'app-payment-source-form-dialog',
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
    FormRoot,
    FormField,
  ],
  templateUrl: './payment-source-form-dialog.html'
})
export class PaymentSourceFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PaymentSourceFormDialogComponent>);
  readonly data = inject<{ paymentSource?: PaymentSource }>(MAT_DIALOG_DATA);

  readonly title = this.data?.paymentSource ? 'Edit Payment Source' : 'New Payment Source';
  readonly submitLabel = this.data?.paymentSource ? 'Save' : 'Create';

  readonly model = signal<PaymentSourceFormModel>({
    name: this.data?.paymentSource?.name ?? '',
  });

  readonly form = form(this.model, (path) => {
    required(path.name, { message: 'Name is required' });
  });

  readonly nameError = computed(() => this.form.name().errors()[0]?.message ?? '');
  readonly submitDisabled = computed(() => this.form().invalid());

  submit(): void {
    if (this.form().invalid()) return;
    this.dialogRef.close(this.model());
  }
}
