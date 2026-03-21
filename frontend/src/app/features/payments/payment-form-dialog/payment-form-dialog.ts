import { Component, inject, OnInit, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Payment } from '../../../core/models/payment.model';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { Payee } from '../../../core/models/payee.model';
import { PaymentFrequency, PAYMENT_FREQUENCY_LABELS } from '../../../core/models/payment-frequency.enum';

@Component({
  selector: 'app-payment-form-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    ReactiveFormsModule,
  ],
  templateUrl: './payment-form-dialog.html'
})
export class PaymentFormDialogComponent implements OnInit {
  readonly dialogRef = inject(MatDialogRef<PaymentFormDialogComponent>);
  readonly data = inject<{
    payment?: Payment;
    paymentSources: PaymentSource[];
    payees: Payee[];
  }>(MAT_DIALOG_DATA);

  readonly PaymentFrequency = PaymentFrequency;
  readonly frequencyOptions = [
    { value: PaymentFrequency.Once, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Once] },
    { value: PaymentFrequency.Monthly, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Monthly] },
    { value: PaymentFrequency.Annually, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Annually] }
  ];

  readonly currencyOptions = ['USD', 'EUR', 'GBP', 'JPY', 'CAD', 'AUD', 'CHF', 'CNY', 'SEK', 'NOK', 'DKK', 'PLN'];

  readonly showEndDate = signal(true);

  readonly form = new FormGroup({
    paymentSourceId: new FormControl(this.data?.payment?.paymentSourceId ?? '', [Validators.required]),
    payeeId: new FormControl(this.data?.payment?.payeeId ?? '', [Validators.required]),
    amount: new FormControl<number | null>(this.data?.payment?.amount ?? null, [
      Validators.required,
      Validators.min(0.01)
    ]),
    currency: new FormControl(this.data?.payment?.currency ?? 'USD', [Validators.required]),
    frequency: new FormControl<PaymentFrequency | null>(
      this.data?.payment?.frequency ?? null,
      [Validators.required]
    ),
    startDate: new FormControl<Date | null>(
      this.data?.payment?.startDate ? new Date(this.data.payment.startDate) : null,
      [Validators.required]
    ),
    endDate: new FormControl<Date | null>(
      this.data?.payment?.endDate ? new Date(this.data.payment.endDate) : null
    )
  });

  ngOnInit(): void {
    this.form.controls.frequency.valueChanges.subscribe(freq => {
      this.showEndDate.set(freq !== PaymentFrequency.Once);
      if (freq === PaymentFrequency.Once) {
        this.form.controls.endDate.setValue(null);
      }
    });

    const initialFreq = this.form.controls.frequency.value;
    this.showEndDate.set(initialFreq !== PaymentFrequency.Once);
  }

  submit(): void {
    if (this.form.valid) {
      const raw = this.form.value;
      const result = {
        paymentSourceId: raw.paymentSourceId,
        payeeId: raw.payeeId,
        amount: raw.amount,
        currency: raw.currency,
        frequency: raw.frequency,
        startDate: (raw.startDate as Date).toISOString().split('T')[0],
        endDate: raw.endDate ? (raw.endDate as Date).toISOString().split('T')[0] : undefined
      };
      this.dialogRef.close(result);
    }
  }
}
