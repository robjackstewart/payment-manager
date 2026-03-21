export interface PaymentSource {
  id: string;
  userId: string;
  name: string;
}

export interface CreatePaymentSourceRequest {
  name: string;
}

export interface UpdatePaymentSourceRequest {
  name: string;
}
