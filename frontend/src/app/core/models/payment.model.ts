import { PaymentFrequency } from './payment-frequency.enum';

export interface UserShare {
  percentage: number;
  value: number;
}

export interface PaymentSplit {
  contactId: string;
  percentage: number;
}

export interface Payment {
  id: string;
  userId: string;
  paymentSourceId: string;
  payeeId: string;
  amount: number;
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
  amount: number;
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
