export interface PaymentSource {
  id: string;
  userId: string;
  name: string;
}

export interface CreatePaymentSourceRequest {
  userId: string;
  name: string;
}

export interface UpdatePaymentSourceRequest {
  userId: string;
  name: string;
}
