export interface Contact {
  id: string;
  userId: string;
  name: string;
}

export interface CreateContactRequest {
  name: string;
}

export interface UpdateContactRequest {
  name: string;
}
