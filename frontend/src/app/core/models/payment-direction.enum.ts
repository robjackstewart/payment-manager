export enum PaymentDirection {
  Outgoing = 0,
  Incoming = 1
}

export const PAYMENT_DIRECTION_LABELS: Record<PaymentDirection, string> = {
  [PaymentDirection.Outgoing]: 'Outgoing',
  [PaymentDirection.Incoming]: 'Incoming'
};
