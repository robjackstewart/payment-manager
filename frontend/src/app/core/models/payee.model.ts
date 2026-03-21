export interface Payee {
  id: string;
  userId: string;
  name: string;
}

export interface CreatePayeeRequest {
  userId: string;
  name: string;
}

export interface UpdatePayeeRequest {
  userId: string;
  name: string;
}
