export interface Person {
  id: string;
  userId: string;
  name: string;
}

export interface CreatePersonRequest {
  name: string;
}

export interface UpdatePersonRequest {
  name: string;
}
