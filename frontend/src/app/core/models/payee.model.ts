export interface Payee {
  id: string;
  userId: string;
  name: string;
}

export interface CreatePayeeRequest {
  name: string;
}

export interface UpdatePayeeRequest {
  name: string;
}
