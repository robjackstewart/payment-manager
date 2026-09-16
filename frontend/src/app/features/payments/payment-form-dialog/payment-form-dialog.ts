import { Component, computed, effect, inject, untracked } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
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
import { Person } from '../../../core/models/person.model';
import { PayerGroup } from '../../../core/models/payer-group.model';
import { PaymentFrequency, PAYMENT_FREQUENCY_LABELS } from '../../../core/models/payment-frequency.enum';
import { PaymentDirection } from '../../../core/models/payment-direction.enum';

@Component({
  selector: 'app-payment-form-dialog',
  standalone: true,
  providers: [provideNativeDateAdapter()],
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
    ReactiveFormsModule
  ],
  templateUrl: './payment-form-dialog.html'
})
export class PaymentFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PaymentFormDialogComponent>);
  readonly data = inject<{
    payment?: Payment;
    direction?: PaymentDirection;
    paymentSources: PaymentSource[];
    payees: Payee[];
    people: Person[];
    payerGroups: PayerGroup[];
  }>(MAT_DIALOG_DATA);

  readonly direction = this.data.direction ?? PaymentDirection.Outgoing;
  readonly isIncoming = this.direction === PaymentDirection.Incoming;

  readonly title = this.data.payment
    ? (this.isIncoming ? 'Edit Income' : 'Edit Payment')
    : (this.isIncoming ? 'New Income' : 'New Payment');
  readonly submitLabel = this.data.payment ? 'Save' : 'Create';
  readonly isEditing = !!this.data.payment;

  // Payee/PaymentSource are symmetric concepts read in opposite directions.
  readonly paymentSourceLabel = this.isIncoming ? 'Paid Into' : 'Paid From';
  readonly payeeLabel = this.isIncoming ? 'Paid By' : 'Paid To';
  readonly splitSectionTitle = this.isIncoming ? 'Income Split' : 'Bill Split';
  readonly descriptionPlaceholder = this.isIncoming ? 'What is this income for?' : 'What is this payment for?';
  readonly noGroupHint = 'Split this with individual people, or assign it to a payer group.';

  readonly PaymentFrequency = PaymentFrequency;
  readonly frequencyOptions = [
    { value: PaymentFrequency.Once, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Once] },
    { value: PaymentFrequency.Monthly, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Monthly] },
    { value: PaymentFrequency.Annually, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Annually] }
  ];

  readonly currencyOptions = ['USD', 'EUR', 'GBP', 'JPY', 'CAD', 'AUD', 'CHF', 'CNY', 'SEK', 'NOK', 'DKK', 'PLN'];

  readonly payerGroups = this.data.payerGroups ?? [];
  readonly people = this.data.people ?? [];

  readonly form = new FormGroup({
    paymentSourceId: new FormControl(this.data?.payment?.paymentSourceId ?? '', [Validators.required]),
    payeeId: new FormControl(this.data?.payment?.payeeId ?? '', [Validators.required]),
    // Income belongs to people, not groups (see PaymentSplitGuard) — it never carries a group.
    payerGroupId: new FormControl<string | null>(
      this.isIncoming ? null : (this.data?.payment?.payerGroupId ?? null)
    ),
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
    splits: new FormArray(this.initialSplitRows()),
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

  private readonly splitsValue = toSignal(
    this.splits.valueChanges as Observable<{ personId: string; percentage: number | null }[]>,
    { initialValue: this.splits.value as { personId: string; percentage: number | null }[] }
  );

  private readonly formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

  /** Splits sum to exactly 100 (floored to 2dp so 33.33 + 33.33 + 33.34 passes). */
  readonly splitsTotal = computed(() =>
    this.splitsValue().reduce((sum, s) => sum + (Number(s.percentage) || 0), 0)
  );

  readonly splitsTotalDisplay = computed(() => {
    const value = this.splitsTotal();
    return `${value % 1 === 0 ? value.toFixed(0) : value.toFixed(2)}%`;
  });

  readonly splitsSumValid = computed(() => Math.round(this.splitsTotal() * 100) / 100 === 100);

  readonly hasDuplicatePeople = computed(() => {
    const ids = this.splitsValue().map(s => s.personId).filter(id => !!id);
    return new Set(ids).size !== ids.length;
  });

  readonly submitDisabled = computed(() =>
    this.formStatus() !== 'VALID' || !this.splitsSumValid() || this.hasDuplicatePeople()
  );

  private readonly payerGroupIdValue = toSignal(
    this.form.controls.payerGroupId.valueChanges,
    { initialValue: this.form.controls.payerGroupId.value }
  );

  /**
   * Splits may name any of the user's people. A payer group only narrows the choice to that
   * group's members; income never carries a group.
   */
  readonly splitPersonOptions = computed<Person[]>(() => {
    const groupId = this.isIncoming ? null : this.payerGroupIdValue();
    if (!groupId) return this.people;
    const group = this.payerGroups.find(g => g.id === groupId);
    if (!group) return [];
    return this.people.filter(p => group.memberPersonIds.includes(p.id));
  });

  readonly splitHint = computed(() => {
    const groupId = this.isIncoming ? null : this.payerGroupIdValue();
    return groupId
      ? 'This payer group has no members yet. Add people to it on the People page.'
      : `${this.noGroupHint} Add people on the People page first.`;
  });

  private readonly frequency = toSignal(
    this.form.controls.frequency.valueChanges,
    { initialValue: this.form.controls.frequency.value }
  );

  readonly showEndDate = computed(() => this.frequency() !== PaymentFrequency.Once);

  constructor() {
    effect(() => {
      if (this.frequency() === PaymentFrequency.Once) {
        untracked(() => this.form.controls.endDate.setValue(null));
      }
    });

    // Changing the group invalidates the current splits (see backend PaymentSplitGuard).
    // valueChanges never emits for the constructor's initial value, so edit-mode's pre-filled
    // splits survive until the user picks a different group.
    this.form.controls.payerGroupId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.splits.clear());
  }

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

  private static parseDateOnly(value: string): Date {
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  /**
   * Edit mode keeps the payment's existing splits. A new payment starts empty — the owner is not
   * special, so there is no sensible person to pre-select; the user picks the participants.
   */
  private initialSplitRows(): FormGroup[] {
    return (this.data?.payment?.splits ?? []).map(s => this.createSplitRow(s.personId, s.percentage));
  }

  private createSplitRow(personId = '', percentage: number | null = null): FormGroup {
    return new FormGroup({
      personId: new FormControl(personId, [Validators.required]),
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

  private readonly pendingRemovals: string[] = [];

  removeValue(index: number): void {
    const group = this.values.at(index) as FormGroup;
    if (group.controls['isExisting']?.value) {
      const original = group.controls['originalEffectiveDate']?.value as Date | null;
      if (original) {
        this.pendingRemovals.push(original.toISOString().split('T')[0]);
      }
    }
    this.values.removeAt(index);
  }

  submit(): void {
    if (this.submitDisabled()) return;

    const raw = this.form.getRawValue();
    const startDateStr = (raw.startDate as Date).toISOString().split('T')[0];
    const endDateStr = raw.endDate ? (raw.endDate as Date).toISOString().split('T')[0] : undefined;
    const splits = (raw.splits as { personId: string; percentage: number }[]).map(s => ({
      personId: s.personId,
      percentage: Number(s.percentage)
    }));

    const metadata = {
      paymentSourceId: raw.paymentSourceId!,
      payeeId: raw.payeeId!,
      currency: raw.currency!,
      frequency: raw.frequency!,
      direction: this.direction,
      startDate: startDateStr,
      endDate: endDateStr,
      description: raw.description || undefined,
      payerGroupId: this.isIncoming ? null : raw.payerGroupId,
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
      const valuesToRemove = [
        ...this.pendingRemovals,
        ...allValues
          .filter(v => v.isExisting && v.originalEffectiveDate != null &&
            (v.effectiveDate as Date).toISOString().split('T')[0] !== (v.originalEffectiveDate as Date).toISOString().split('T')[0])
          .map(v => (v.originalEffectiveDate as Date).toISOString().split('T')[0])
      ];
      this.dialogRef.close({ metadataRequest: { ...metadata, initialAmount: Number(raw.amount) }, valuesToUpsert, valuesToRemove });
    } else {
      this.dialogRef.close({ ...metadata, amount: Number(raw.amount) });
    }
  }
}
