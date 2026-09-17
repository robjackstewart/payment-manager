import { PaymentFrequency } from './payment-frequency.enum';
import { PaymentDirection } from './payment-direction.enum';

export interface PaymentSplit {
  personId: string;
  percentage: number;
  value?: number;
}

export interface EffectivePaymentValue {
  effectiveDate: string;
  amount: number;
}

export interface AddPaymentValueRequest {
  effectiveDate: string;
  amount: number;
}

/** A complete split set that takes effect on `effectiveDate`, overriding the initial split. */
export interface PaymentSplitVersion {
  effectiveDate: string;
  splits: PaymentSplit[];
}

export interface AddPaymentSplitsRequest {
  effectiveDate: string;
  splits: PaymentSplit[];
}

export interface Payment {
  id: string;
  userId: string;
  paymentSourceId: string;
  payeeId: string;
  currentAmount: number;
  initialAmount: number;
  values: EffectivePaymentValue[];
  currency: string;
  frequency: PaymentFrequency;
  direction: PaymentDirection;
  startDate: string;
  endDate?: string;
  description?: string;
  payerGroupId?: string | null;
  /** The split in effect today (resolved server-side). */
  splits: PaymentSplit[];
  /** The initial split, in effect from the payment's start date. */
  initialSplits: PaymentSplit[];
  /** Dated split sets that override the initial split from their effective date. */
  splitVersions: PaymentSplitVersion[];
}

export interface CreatePaymentRequest {
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  currency: string;
  frequency: PaymentFrequency;
  direction: PaymentDirection;
  startDate: string;
  endDate?: string;
  description?: string;
  payerGroupId?: string | null;
  splits?: PaymentSplit[];
}

export interface UpdatePaymentRequest {
  paymentSourceId: string;
  payeeId: string;
  initialAmount: number;
  currency: string;
  frequency: PaymentFrequency;
  direction: PaymentDirection;
  startDate: string;
  endDate?: string;
  description?: string;
  payerGroupId?: string | null;
  splits?: PaymentSplit[];
}

export interface PaymentOccurrence {
  paymentId: string;
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  currency: string;
  frequency: PaymentFrequency;
  direction: PaymentDirection;
  occurrenceDate: string;
  startDate: string;
  endDate?: string;
  description?: string;
  payerGroupId?: string | null;
  splits: PaymentSplit[];
}

export interface OccurrenceSummaryPersonAmount {
  personId: string;
  amount: number;
}

export interface OccurrenceSummaryPayeeAmount {
  payeeId: string;
  amount: number;
}

export interface OccurrenceSummaryPaymentSourceBreakdown {
  paymentSourceId: string;
  totalAmount: number;
  personTotals: OccurrenceSummaryPersonAmount[];
}

/** Totals for one direction (outgoing or incoming) within a currency. */
export interface DirectionTotals {
  totalAmount: number;
  personTotals: OccurrenceSummaryPersonAmount[];
  byPaymentSource: OccurrenceSummaryPaymentSourceBreakdown[];
}

/** Incoming minus outgoing — the "available cash" figure. */
export interface NetTotals {
  totalAmount: number;
  personTotals: OccurrenceSummaryPersonAmount[];
}

export interface CurrencySummary {
  currency: string;
  outgoing: DirectionTotals;
  incoming: DirectionTotals;
  net: NetTotals;
  outgoingByPayee: OccurrenceSummaryPayeeAmount[];
}

/** One entry per real payer group with payments in range. Ungrouped payments are not a group —
 * their per-person shares appear in the `people` commitments. */
export interface GroupSummary {
  payerGroupId: string;
  currencies: CurrencySummary[];
}

/** A person's income, committed outgoing share, and headroom across every payer group they
 * belong to plus any personal payments, for one currency. */
export interface PersonCommitment {
  personId: string;
  currency: string;
  income: number;
  committed: number;
  remaining: number;
  isOverCommitted: boolean;
}

export interface PaymentOccurrencesResponse {
  occurrences: PaymentOccurrence[];
  summary: GroupSummary[];
  people: PersonCommitment[];
}
