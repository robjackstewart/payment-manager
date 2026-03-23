import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { MatOption, provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';
import { MatIcon } from '@angular/material/icon';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Payment } from '../../../core/models/payment.model';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { Payee } from '../../../core/models/payee.model';
import { Contact } from '../../../core/models/contact.model';
import { PaymentFrequency, PAYMENT_FREQUENCY_LABELS } from '../../../core/models/payment-frequency.enum';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-payment-form-dialog',
  standalone: true,
  providers: [provideNativeDateAdapter(), CurrencyPipe, DatePipe],
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
    DatePipe,
    CurrencyPipe,
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
  readonly isEditing = !!this.data.payment;

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
    currency: new FormControl(this.data?.payment?.currency ?? 'USD', [Validators.required]),
    frequency: new FormControl<PaymentFrequency | null>(
      this.data?.payment?.frequency ?? null,
      [Validators.required]
    ),
    startDate: new FormControl<Date | null>(
      this.data?.payment?.startDate ? PaymentFormDialogComponent.parseDateOnly(this.data.payment.startDate) : null,
      [Validators.required]
    ),
    endDate: new FormControl<Date | null>(
      this.data?.payment?.endDate ? PaymentFormDialogComponent.parseDateOnly(this.data.payment.endDate) : null
    ),
    description: new FormControl(this.data?.payment?.description ?? '', [Validators.maxLength(500)]),
    splits: new FormArray(
      (this.data?.payment?.splits ?? []).map(s => this.createSplitRow(s.contactId, s.percentage))
    ),
    // Amount field: required in both create and edit modes
    amount: new FormControl<number | null>(
      this.data?.payment?.initialAmount ?? this.data?.payment?.currentAmount ?? null,
      [Validators.required, Validators.min(0.01)]
    ),
    // Edit mode: all effective values (existing with disabled date + new with editable date)
    values: new FormArray(
      this.isEditing
        ? (this.data.payment?.values ?? []).map(v =>
            this.createValueRow(PaymentFormDialogComponent.parseDateOnly(v.effectiveDate), v.amount, true))
        : []
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

  get values(): FormArray {
    return this.form.get('values') as FormArray;
  }

  get valueControls(): AbstractControl[] {
    return this.values.controls;
  }

  isExistingValue(index: number): boolean {
    return !!(this.values.at(index) as FormGroup).controls['isExisting']?.value;
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

  private static parseDateOnly(value: string): Date {
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  private createSplitRow(contactId = '', percentage: number | null = null): FormGroup {
    return new FormGroup({
      contactId: new FormControl(contactId, [Validators.required]),
      percentage: new FormControl<number | null>(percentage, [Validators.required, Validators.min(0.01), Validators.max(100)])
    });
  }

  private createValueRow(effectiveDate: Date | null = null, amount: number | null = null, isExisting = false): FormGroup {
    return new FormGroup({
      effectiveDate: new FormControl<Date | null>(effectiveDate, [Validators.required]),
      amount: new FormControl<number | null>(amount, [Validators.required, Validators.min(0.01)]),
      isExisting: new FormControl(isExisting),
      originalEffectiveDate: new FormControl<Date | null>(effectiveDate),
    });
  }

  addSplit(): void {
    this.splits.push(this.createSplitRow());
  }

  removeSplit(index: number): void {
    this.splits.removeAt(index);
  }

  addValue(): void {
    this.values.push(this.createValueRow());
  }

  removeValue(index: number): void {
    this.values.removeAt(index);
  }

  submit(): void {
    if (this.form.valid) {
      const raw = this.form.getRawValue();
      const startDateStr = (raw.startDate as Date).toISOString().split('T')[0];
      const endDateStr = raw.endDate ? (raw.endDate as Date).toISOString().split('T')[0] : undefined;
      const splits = (raw.splits as { contactId: string; percentage: number }[])?.map(s => ({
        contactId: s.contactId,
        percentage: Number(s.percentage)
      })) ?? [];

      const metadata = {
        paymentSourceId: raw.paymentSourceId!,
        payeeId: raw.payeeId!,
        currency: raw.currency!,
        frequency: raw.frequency!,
        startDate: startDateStr,
        endDate: endDateStr,
        description: raw.description || undefined,
        splits,
      };

      if (this.isEditing) {
        const allValues = raw.values as { effectiveDate: Date; amount: number; isExisting: boolean; originalEffectiveDate: Date | null }[];
        const valuesToUpsert = allValues
          .filter(v => v.effectiveDate != null)
          .map(v => ({
            effectiveDate: (v.effectiveDate as Date).toISOString().split('T')[0],
            amount: Number(v.amount),
          }));
        const valuesToRemove = allValues
          .filter(v => v.isExisting && v.originalEffectiveDate != null &&
            (v.effectiveDate as Date).toISOString().split('T')[0] !== (v.originalEffectiveDate as Date).toISOString().split('T')[0])
          .map(v => (v.originalEffectiveDate as Date).toISOString().split('T')[0]);
        this.dialogRef.close({ metadataRequest: { ...metadata, initialAmount: Number(raw.amount) }, valuesToUpsert, valuesToRemove });
      } else {
        this.dialogRef.close({ ...metadata, amount: Number(raw.amount) });
      }
    }
  }
}
