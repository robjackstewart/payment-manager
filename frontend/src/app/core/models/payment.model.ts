import { PaymentFrequency } from './payment-frequency.enum';

export interface Payment {
  id: string;
  userId: string;
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  frequency: PaymentFrequency;
  startDate: string;
  endDate?: string;
}

export interface CreatePaymentRequest {
  userId: string;
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  frequency: PaymentFrequency;
  startDate: string;
  endDate?: string;
}

export interface UpdatePaymentRequest {
  userId: string;
  paymentSourceId: string;
  payeeId: string;
  amount: number;
  frequency: PaymentFrequency;
  startDate: string;
  endDate?: string;
}
