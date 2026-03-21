export interface User {
  id: string;
  name: string;
}

export interface CreateUserRequest {
  name: string;
}

export interface UpdateUserRequest {
  name: string;
}
