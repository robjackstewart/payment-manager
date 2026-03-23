import { PaymentFrequency } from './payment-frequency.enum';

export interface UserShare {
  percentage: number;
  value: number;
}

export interface PaymentSplit {
  contactId: string;
  percentage: number;
}

export interface EffectivePaymentValue {
  effectiveDate: string;
  amount: number;
}

export interface AddPaymentValueRequest {
  effectiveDate: string;
  amount: number;
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
  startDate: string;
  endDate?: string;
  description?: string;
  userShare: UserShare;
  splits: PaymentSplit[];
}

export interface CreatePaymentRequest {
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  currency: string;
  frequency: PaymentFrequency;
  startDate: string;
  endDate?: string;
  description?: string;
  splits?: PaymentSplit[];
}

export interface UpdatePaymentRequest {
  paymentSourceId: string;
  payeeId: string;
  initialAmount: number;
  currency: string;
  frequency: PaymentFrequency;
  startDate: string;
  endDate?: string;
  description?: string;
  splits?: PaymentSplit[];
}

export interface PaymentOccurrence {
  paymentId: string;
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  currency: string;
  frequency: PaymentFrequency;
  occurrenceDate: string;
  startDate: string;
  endDate?: string;
  description?: string;
  userShare: UserShare;
  splits: PaymentSplit[];
}

export interface OccurrenceSummaryContactAmount {
  contactId: string;
  amount: number;
}

export interface OccurrenceSummaryPaymentSourceBreakdown {
  paymentSourceId: string;
  totalAmount: number;
  userTotal: number;
  contactTotals: OccurrenceSummaryContactAmount[];
}

export interface OccurrenceSummary {
  currency: string;
  totalAmount: number;
  userTotal: number;
  contactTotals: OccurrenceSummaryContactAmount[];
  byPaymentSource: OccurrenceSummaryPaymentSourceBreakdown[];
}

export interface PaymentOccurrencesResponse {
  occurrences: PaymentOccurrence[];
  summary: OccurrenceSummary[];
}

