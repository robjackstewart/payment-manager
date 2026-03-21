import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import { User, CreateUserRequest, UpdateUserRequest } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(): Observable<User[]> {
    return this.api.getAllUsers().pipe(map(r => r.users as User[]));
  }

  getById(id: string): Observable<User> {
    return this.api.getUser(id) as Observable<User>;
  }

  create(req: CreateUserRequest): Observable<User> {
    return this.api.createUser(req) as Observable<User>;
  }

  update(id: string, req: UpdateUserRequest): Observable<User> {
    return this.api.updateUser(id, req) as Observable<User>;
  }

  delete(id: string): Observable<void> {
    return this.api.deleteUser(id) as Observable<void>;
  }
}
