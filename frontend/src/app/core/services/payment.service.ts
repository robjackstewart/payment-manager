import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import {
  CreatePaymentRequest,
  Payment,
  PaymentOccurrence,
  UpdatePaymentRequest
} from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(): Observable<Payment[]> {
    return this.api.getAllPayments().pipe(
      map(r => r.payments.map(p => ({ ...p, amount: Number(p.amount) } as Payment)))
    );
  }

  getOccurrences(from: string, to: string): Observable<PaymentOccurrence[]> {
    return this.api.getPaymentOccurrences(from, to).pipe(
      map(r => r.occurrences.map(o => ({ ...o, amount: Number(o.amount) } as PaymentOccurrence)))
    );
  }

  getById(id: string): Observable<Payment> {
    return this.api.getPayment(id) as Observable<Payment>;
  }

  create(req: CreatePaymentRequest): Observable<Payment> {
    return this.api.createPayment({
      ...req,
      endDate: req.endDate ?? null,
      splits: req.splits?.map(s => ({ contactId: s.contactId, percentage: s.percentage })) ?? null,
    }) as Observable<Payment>;
  }

  update(id: string, req: UpdatePaymentRequest): Observable<Payment> {
    return this.api.updatePayment(id, {
      ...req,
      endDate: req.endDate ?? null,
      splits: req.splits?.map(s => ({ contactId: s.contactId, percentage: s.percentage })) ?? null,
    }) as Observable<Payment>;
  }

  delete(id: string): Observable<void> {
    return this.api.deletePayment(id) as Observable<void>;
  }
}
