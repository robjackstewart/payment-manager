import { PaymentFrequency } from './payment-frequency.enum';

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
}

export interface CreatePaymentRequest {
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  currency: string;
  frequency: PaymentFrequency;
  startDate: string;
  endDate?: string;
}

export interface UpdatePaymentRequest {
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  currency: string;
  frequency: PaymentFrequency;
  startDate: string;
  endDate?: string;
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
}
