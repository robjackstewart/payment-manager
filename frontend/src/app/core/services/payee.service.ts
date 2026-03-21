import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import { Payee, CreatePayeeRequest, UpdatePayeeRequest } from '../models/payee.model';

@Injectable({ providedIn: 'root' })
export class PayeeService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(): Observable<Payee[]> {
    return this.api.getAllPayees().pipe(map(r => r.payees as Payee[]));
  }

  getById(id: string): Observable<Payee> {
    return this.api.getPayee(id) as Observable<Payee>;
  }

  create(req: CreatePayeeRequest): Observable<Payee> {
    return this.api.createPayee(req) as Observable<Payee>;
  }

  update(id: string, req: UpdatePayeeRequest): Observable<Payee> {
    return this.api.updatePayee(id, req) as Observable<Payee>;
  }

  delete(id: string): Observable<void> {
    return this.api.deletePayee(id) as Observable<void>;
  }
}
