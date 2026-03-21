export enum PaymentFrequency {
  Once = 0,
  Monthly = 1,
  Annually = 2
}

export const PAYMENT_FREQUENCY_LABELS: Record<PaymentFrequency, string> = {
  [PaymentFrequency.Once]: 'Once',
  [PaymentFrequency.Monthly]: 'Monthly',
  [PaymentFrequency.Annually]: 'Annually'
};
