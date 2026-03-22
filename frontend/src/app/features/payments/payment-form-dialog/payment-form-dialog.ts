import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { MatOption } from '@angular/material/core';
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';
import { MatIcon } from '@angular/material/icon';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Payment } from '../../../core/models/payment.model';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { Payee } from '../../../core/models/payee.model';
import { Contact } from '../../../core/models/contact.model';
import { PaymentFrequency, PAYMENT_FREQUENCY_LABELS } from '../../../core/models/payment-frequency.enum';

@Component({
  selector: 'app-payment-form-dialog',
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatIconButton,
    MatFormField,
    MatLabel,
    MatError,
    MatSuffix,
    MatInput,
    MatSelect,
    MatOption,
    MatDatepicker,
    MatDatepickerInput,
    MatDatepickerToggle,
    MatIcon,
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
    contacts: Contact[];
  }>(MAT_DIALOG_DATA);

  readonly title = this.data.payment ? 'Edit Payment' : 'New Payment';
  readonly submitLabel = this.data.payment ? 'Save' : 'Create';

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
    ),
    description: new FormControl(this.data?.payment?.description ?? '', [Validators.maxLength(500)]),
    splits: new FormArray(
      (this.data?.payment?.splits ?? []).map(s => this.createSplitRow(s.contactId, s.percentage))
    )
  });

  readonly yourShare = computed(() => {
    const total = this.splits.controls.reduce((sum, ctrl) => {
      return sum + (Number(ctrl.get('percentage')?.value) || 0);
    }, 0);
    return Math.max(0, 100 - total);
  });

  readonly yourShareDisplay = computed(() => {
    const value = this.yourShare();
    return `${value % 1 === 0 ? value.toFixed(0) : value.toFixed(2)}%`;
  });

  get splits(): FormArray {
    return this.form.get('splits') as FormArray;
  }

  get splitControls(): AbstractControl[] {
    return this.splits.controls;
  }

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

  private createSplitRow(contactId = '', percentage: number | null = null): FormGroup {
    return new FormGroup({
      contactId: new FormControl(contactId, [Validators.required]),
      percentage: new FormControl<number | null>(percentage, [Validators.required, Validators.min(0.01), Validators.max(100)])
    });
  }

  addSplit(): void {
    this.splits.push(this.createSplitRow());
  }

  removeSplit(index: number): void {
    this.splits.removeAt(index);
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
        endDate: raw.endDate ? (raw.endDate as Date).toISOString().split('T')[0] : undefined,
        description: raw.description || undefined,
        splits: (raw.splits as { contactId: string; percentage: number }[])?.map(s => ({
          contactId: s.contactId,
          contactName: this.data.contacts.find(c => c.id === s.contactId)?.name ?? '',
          percentage: Number(s.percentage)
        })) ?? []
      };
      this.dialogRef.close(result);
    }
  }
}
