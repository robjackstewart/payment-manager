import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import {
  CreatePaymentRequest,
  Payment,
  UpdatePaymentRequest
} from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(userId: string): Observable<Payment[]> {
    return this.api.getAllPayments(userId).pipe(
      map(r => r.payments.map(p => ({ ...p, amount: Number(p.amount) } as Payment)))
    );
  }

  getById(id: string): Observable<Payment> {
    return this.api.getPayment(id) as Observable<Payment>;
  }

  create(req: CreatePaymentRequest): Observable<Payment> {
    return this.api.createPayment({
      ...req,
      endDate: req.endDate ?? null,
    }) as Observable<Payment>;
  }

  update(id: string, req: UpdatePaymentRequest): Observable<Payment> {
    return this.api.updatePayment(id, {
      ...req,
      endDate: req.endDate ?? null,
    }) as Observable<Payment>;
  }

  delete(id: string): Observable<void> {
    return this.api.deletePayment(id) as Observable<void>;
  }
}
