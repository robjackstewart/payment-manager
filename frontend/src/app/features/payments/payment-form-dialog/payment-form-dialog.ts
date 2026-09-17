import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { MatOption, provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';
import { MatIcon } from '@angular/material/icon';
import { FormField, FormRoot, applyEach, form, max, maxLength, min, required } from '@angular/forms/signals';
import { Payment } from '../../../core/models/payment.model';
import { PaymentSource } from '../../../core/models/payment-source.model';
import { Payee } from '../../../core/models/payee.model';
import { Person } from '../../../core/models/person.model';
import { PayerGroup } from '../../../core/models/payer-group.model';
import { PaymentFrequency, PAYMENT_FREQUENCY_LABELS } from '../../../core/models/payment-frequency.enum';
import { PaymentDirection } from '../../../core/models/payment-direction.enum';

interface SplitModel {
  personId: string;
  percentage: number | null;
}

interface ValueModel {
  effectiveDate: Date | null;
  amount: number | null;
  isExisting: boolean;
  originalEffectiveDate: Date | null;
}

interface SplitVersionModel {
  effectiveDate: Date | null;
  splits: SplitModel[];
  isExisting: boolean;
  originalEffectiveDate: Date | null;
}

interface PaymentFormModel {
  paymentSourceId: string;
  payeeId: string;
  payerGroupId: string | null;
  currency: string;
  frequency: PaymentFrequency | null;
  startDate: Date | null;
  endDate: Date | null;
  description: string;
  amount: number | null;
  splits: SplitModel[];
  values: ValueModel[];
  splitVersions: SplitVersionModel[];
}

@Component({
  selector: 'app-payment-form-dialog',
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
    FormRoot,
    FormField
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
  readonly isOutgoing = !this.isIncoming;

  readonly title = this.data.payment
    ? (this.isIncoming ? 'Edit Income' : 'Edit Payment')
    : (this.isIncoming ? 'New Income' : 'New Payment');
  readonly submitLabel = this.data.payment ? 'Save' : 'Create';
  readonly isEditing = !!this.data.payment;

  // Payee/PaymentSource are symmetric concepts read in opposite directions.
  readonly paymentSourceLabel = this.isIncoming ? 'Paid Into' : 'Paid From';
  readonly payeeLabel = this.isIncoming ? 'Paid By' : 'Paid To';
  // Income is owned outright by one person; bills may be split across people or a group.
  readonly splitSectionTitle = this.isIncoming ? 'Income For' : 'Bill Split';
  readonly descriptionPlaceholder = this.isIncoming ? 'What is this income for?' : 'What is this payment for?';
  readonly noGroupHint = 'Split this with individual people, or assign it to a payer group.';
  readonly incomeHint = 'Income belongs to one person. Add people on the People page first.';

  readonly PaymentFrequency = PaymentFrequency;
  readonly frequencyOptions = [
    { value: PaymentFrequency.Once, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Once] },
    { value: PaymentFrequency.Monthly, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Monthly] },
    { value: PaymentFrequency.Annually, label: PAYMENT_FREQUENCY_LABELS[PaymentFrequency.Annually] }
  ];

  readonly currencyOptions = ['USD', 'EUR', 'GBP', 'JPY', 'CAD', 'AUD', 'CHF', 'CNY', 'SEK', 'NOK', 'DKK', 'PLN'];

  readonly payerGroups = this.data.payerGroups ?? [];
  readonly people = this.data.people ?? [];

  readonly model = signal<PaymentFormModel>({
    paymentSourceId: this.data?.payment?.paymentSourceId ?? '',
    payeeId: this.data?.payment?.payeeId ?? '',
    // Income belongs to people, not groups (see PaymentSplitGuard) — it never carries a group.
    payerGroupId: this.isIncoming ? null : (this.data?.payment?.payerGroupId ?? null),
    currency: this.data?.payment?.currency ?? 'USD',
    frequency: this.data?.payment?.frequency ?? null,
    startDate: this.data?.payment?.startDate ? PaymentFormDialogComponent.parseDateOnly(this.data.payment.startDate) : null,
    endDate: this.data?.payment?.endDate ? PaymentFormDialogComponent.parseDateOnly(this.data.payment.endDate) : null,
    description: this.data?.payment?.description ?? '',
    // Amount field: required in both create and edit modes
    amount: this.data?.payment?.initialAmount ?? this.data?.payment?.currentAmount ?? null,
    // Income always carries exactly one 100% split — the person it belongs to (see
    // PaymentSplitGuard). Bills keep their existing splits; a new bill starts empty because the
    // owner is not special, so there is no sensible person to pre-select.
    splits: this.isIncoming
      ? [{ personId: this.data?.payment?.initialSplits?.[0]?.personId ?? this.data?.payment?.splits?.[0]?.personId ?? '', percentage: 100 }]
      : (this.data?.payment?.initialSplits ?? []).map(s => ({ personId: s.personId, percentage: s.percentage })),
    values: this.isEditing
      ? (this.data.payment?.values ?? []).map(v =>
          this.createValueRow(PaymentFormDialogComponent.parseDateOnly(v.effectiveDate), v.amount, true))
      : [],
    // Dated split changes only apply to existing outgoing payments; a new payment starts with
    // just its initial split.
    splitVersions: this.isEditing && this.isOutgoing
      ? (this.data.payment?.splitVersions ?? []).map(v =>
          this.createSplitVersionRow(
            PaymentFormDialogComponent.parseDateOnly(v.effectiveDate),
            v.splits.map(s => ({ personId: s.personId, percentage: s.percentage })),
            true))
      : []
  });

  readonly form = form(this.model, (path) => {
    required(path.paymentSourceId, { message: 'This field is required' });
    required(path.payeeId, { message: 'This field is required' });
    required(path.currency, { message: 'Currency is required' });
    required(path.frequency, { message: 'Frequency is required' });
    required(path.startDate, { message: 'Start date is required' });
    maxLength(path.description, 500, { message: 'Description cannot exceed 500 characters' });
    required(path.amount, { message: 'Amount is required' });
    min(path.amount, 0.01, { message: 'Amount must be at least 0.01' });

    applyEach(path.splits, (split) => {
      required(split.personId);
      required(split.percentage);
      min(split.percentage, 0.01);
      max(split.percentage, 100);
    });

    applyEach(path.values, (value) => {
      required(value.effectiveDate);
      required(value.amount);
      min(value.amount, 0.01);
    });

    applyEach(path.splitVersions, (version) => {
      required(version.effectiveDate);
      applyEach(version.splits, (split) => {
        required(split.personId);
        required(split.percentage);
        min(split.percentage, 0.01);
        max(split.percentage, 100);
      });
    });
  });

  readonly paymentSourceError = computed(() => this.form.paymentSourceId().errors()[0]?.message ?? '');
  readonly payeeError = computed(() => this.form.payeeId().errors()[0]?.message ?? '');
  readonly currencyError = computed(() => this.form.currency().errors()[0]?.message ?? '');
  readonly frequencyError = computed(() => this.form.frequency().errors()[0]?.message ?? '');
  readonly startDateError = computed(() => this.form.startDate().errors()[0]?.message ?? '');
  readonly descriptionError = computed(() => this.form.description().errors()[0]?.message ?? '');
  readonly amountError = computed(() => this.form.amount().errors()[0]?.message ?? '');
  readonly incomePersonError = computed(() =>
    this.form.splits[0].personId().errors()[0]?.message ?? ''
  );

  private readonly frequency = computed(() => this.model().frequency);
  private readonly payerGroupId = computed(() => this.model().payerGroupId);

  readonly showEndDate = computed(() => this.frequency() !== PaymentFrequency.Once);

  readonly splits = computed(() => this.model().splits);
  readonly values = computed(() => this.model().values);
  readonly splitVersions = computed(() => this.model().splitVersions);

  /** Splits sum to exactly 100 (floored to 2dp so 33.33 + 33.33 + 33.34 passes). */
  readonly splitsTotal = computed(() =>
    this.splits().reduce((sum, s) => sum + (Number(s.percentage) || 0), 0)
  );

  readonly splitsTotalDisplay = computed(() => {
    const value = this.splitsTotal();
    return `${value % 1 === 0 ? value.toFixed(0) : value.toFixed(2)}%`;
  });

  readonly splitsSumValid = computed(() => Math.round(this.splitsTotal() * 100) / 100 === 100);
  readonly splitsTotalInvalid = computed(() => !this.splitsSumValid());

  readonly hasDuplicatePeople = computed(() => {
    const ids = this.splits().map(s => s.personId).filter(id => !!id);
    return new Set(ids).size !== ids.length;
  });

  /** Per-version totals, displays and validity for the dated split changes. */
  private readonly splitVersionTotals = computed(() =>
    this.splitVersions().map(v => v.splits.reduce((sum, s) => sum + (Number(s.percentage) || 0), 0))
  );

  readonly splitVersionTotalDisplays = computed(() =>
    this.splitVersionTotals().map(value => `${value % 1 === 0 ? value.toFixed(0) : value.toFixed(2)}%`)
  );

  readonly splitVersionTotalsInvalid = computed(() =>
    this.splitVersionTotals().map(value => Math.round(value * 100) / 100 !== 100)
  );

  readonly splitVersionHasDuplicatePeople = computed(() =>
    this.splitVersions().map(v => {
      const ids = v.splits.map(s => s.personId).filter(id => !!id);
      return new Set(ids).size !== ids.length;
    })
  );

  readonly splitVersionsValid = computed(() =>
    this.splitVersionTotalsInvalid().every(invalid => !invalid) &&
    this.splitVersionHasDuplicatePeople().every(duplicate => !duplicate)
  );

  readonly submitDisabled = computed(() =>
    this.form().invalid() || !this.splitsSumValid() || this.hasDuplicatePeople() || !this.splitVersionsValid()
  );

  /**
   * Splits may name any of the user's people. A payer group only narrows the choice to that
   * group's members; income never carries a group.
   */
  readonly splitPersonOptions = computed<Person[]>(() => {
    const groupId = this.isIncoming ? null : this.payerGroupId();
    if (!groupId) return this.people;
    const group = this.payerGroups.find(g => g.id === groupId);
    if (!group) return [];
    return this.people.filter(p => group.memberPersonIds.includes(p.id));
  });

  readonly hasSplitPeople = computed(() => this.splitPersonOptions().length > 0);

  readonly splitHint = computed(() => {
    if (this.isIncoming) return this.incomeHint;
    const groupId = this.payerGroupId();
    return groupId
      ? 'This payer group has no members yet. Add people to it on the People page.'
      : `${this.noGroupHint} Add people on the People page first.`;
  });

  private readonly pendingRemovals: string[] = [];
  private readonly pendingSplitVersionRemovals: string[] = [];

  constructor() {
    // Once-frequency payments have no end date.
    effect(() => {
      if (this.frequency() === PaymentFrequency.Once) {
        untracked(() => {
          if (this.model().endDate !== null) {
            this.model.update(m => ({ ...m, endDate: null }));
          }
        });
      }
    });

    // Changing the group invalidates the current splits (see backend PaymentSplitGuard).
    // The first effect run is skipped so edit-mode's pre-filled splits survive until the user
    // picks a different group.
    let firstRun = true;
    effect(() => {
      this.payerGroupId();
      if (firstRun) {
        firstRun = false;
        return;
      }
      untracked(() => {
        if (this.model().splits.length > 0 || this.model().splitVersions.length > 0) {
          this.model.update(m => ({ ...m, splits: [], splitVersions: [] }));
        }
      });
    });
  }

  addSplit(): void {
    this.model.update(m => ({ ...m, splits: [...m.splits, { personId: '', percentage: null }] }));
  }

  removeSplit(index: number): void {
    this.model.update(m => ({ ...m, splits: m.splits.filter((_, i) => i !== index) }));
  }

  addValue(): void {
    this.model.update(m => ({ ...m, values: [...m.values, this.createValueRow()] }));
  }

  removeValue(index: number): void {
    const value = this.model().values[index];
    if (value?.isExisting && value.originalEffectiveDate) {
      this.pendingRemovals.push(value.originalEffectiveDate.toISOString().split('T')[0]);
    }
    this.model.update(m => ({ ...m, values: m.values.filter((_, i) => i !== index) }));
  }

  addSplitVersion(): void {
    this.model.update(m => ({ ...m, splitVersions: [...m.splitVersions, this.createSplitVersionRow()] }));
  }

  removeSplitVersion(index: number): void {
    const version = this.model().splitVersions[index];
    if (version?.isExisting && version.originalEffectiveDate) {
      this.pendingSplitVersionRemovals.push(version.originalEffectiveDate.toISOString().split('T')[0]);
    }
    this.model.update(m => ({ ...m, splitVersions: m.splitVersions.filter((_, i) => i !== index) }));
  }

  addVersionSplit(versionIndex: number): void {
    this.model.update(m => ({
      ...m,
      splitVersions: m.splitVersions.map((v, i) =>
        i === versionIndex ? { ...v, splits: [...v.splits, { personId: '', percentage: null }] } : v
      )
    }));
  }

  removeVersionSplit(versionIndex: number, splitIndex: number): void {
    this.model.update(m => ({
      ...m,
      splitVersions: m.splitVersions.map((v, i) =>
        i === versionIndex ? { ...v, splits: v.splits.filter((_, j) => j !== splitIndex) } : v
      )
    }));
  }

  submit(): void {
    if (this.submitDisabled()) return;

    const model = this.model();
    const startDateStr = (model.startDate as Date).toISOString().split('T')[0];
    const endDateStr = model.endDate ? model.endDate.toISOString().split('T')[0] : undefined;
    // Income is always a single 100% split; bills keep the percentages the user entered.
    const splits = this.isIncoming
      ? [{ personId: model.splits[0]?.personId ?? '', percentage: 100 }]
      : model.splits.map(s => ({
          personId: s.personId,
          percentage: Number(s.percentage)
        }));

    const metadata = {
      paymentSourceId: model.paymentSourceId,
      payeeId: model.payeeId,
      currency: model.currency,
      frequency: model.frequency!,
      direction: this.direction,
      startDate: startDateStr,
      endDate: endDateStr,
      description: model.description || undefined,
      payerGroupId: this.isIncoming ? null : model.payerGroupId,
      splits,
    };

    if (this.isEditing) {
      const valuesToUpsert = model.values
        .filter(v => v.effectiveDate != null)
        .map(v => ({
          effectiveDate: (v.effectiveDate as Date).toISOString().split('T')[0],
          amount: Number(v.amount),
        }));
      const valuesToRemove = [
        ...this.pendingRemovals,
        ...model.values
          .filter(v => v.isExisting && v.originalEffectiveDate != null &&
            (v.effectiveDate as Date).toISOString().split('T')[0] !== (v.originalEffectiveDate as Date).toISOString().split('T')[0])
          .map(v => (v.originalEffectiveDate as Date).toISOString().split('T')[0])
      ];
      const splitVersionsToUpsert = this.isOutgoing
        ? model.splitVersions
            .filter(v => v.effectiveDate != null)
            .map(v => ({
              effectiveDate: (v.effectiveDate as Date).toISOString().split('T')[0],
              splits: v.splits.map(s => ({ personId: s.personId, percentage: Number(s.percentage) })),
            }))
        : [];
      const splitVersionsToRemove = [
        ...this.pendingSplitVersionRemovals,
        ...model.splitVersions
          .filter(v => v.isExisting && v.originalEffectiveDate != null &&
            (v.effectiveDate as Date).toISOString().split('T')[0] !== (v.originalEffectiveDate as Date).toISOString().split('T')[0])
          .map(v => (v.originalEffectiveDate as Date).toISOString().split('T')[0])
      ];
      this.dialogRef.close({
        metadataRequest: { ...metadata, initialAmount: Number(model.amount) },
        valuesToUpsert,
        valuesToRemove,
        splitVersionsToUpsert,
        splitVersionsToRemove,
      });
    } else {
      this.dialogRef.close({ ...metadata, amount: Number(model.amount) });
    }
  }

  private static parseDateOnly(value: string): Date {
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  private createValueRow(effectiveDate: Date | null = null, amount: number | null = null, isExisting = false): ValueModel {
    return {
      effectiveDate,
      amount,
      isExisting,
      originalEffectiveDate: effectiveDate,
    };
  }

  private createSplitVersionRow(effectiveDate: Date | null = null, splits: SplitModel[] = [], isExisting = false): SplitVersionModel {
    return {
      effectiveDate,
      splits,
      isExisting,
      originalEffectiveDate: effectiveDate,
    };
  }
}
