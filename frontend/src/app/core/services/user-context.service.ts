import { Injectable, signal } from '@angular/core';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserContextService {
  readonly selectedUser = signal<User | null>(null);

  select(user: User): void {
    this.selectedUser.set(user);
  }

  clear(): void {
    this.selectedUser.set(null);
  }
}
