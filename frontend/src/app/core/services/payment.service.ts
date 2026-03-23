import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import {
  AddPaymentValueRequest,
  CreatePaymentRequest,
  Payment,
  PaymentOccurrencesResponse,
  UpdatePaymentRequest
} from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(): Observable<Payment[]> {
    return this.api.getAllPayments().pipe(
      map(r => r.payments.map(p => ({
        ...p,
        currentAmount: Number(p.currentAmount),
        initialAmount: Number(p.initialAmount),
        values: (p.values ?? []).map((v: any) => ({ effectiveDate: v.effectiveDate, amount: Number(v.amount) })),
        userShare: { percentage: Number(p.userShare.percentage), value: Number(p.userShare.value) },
      } as Payment)))
    );
  }

  getOccurrences(from: string, to: string): Observable<PaymentOccurrencesResponse> {
    return this.api.getPaymentOccurrences(from, to).pipe(
      map(r => ({
        occurrences: r.occurrences.map(o => ({
          ...o,
          amount: Number(o.amount),
          userShare: { percentage: Number(o.userShare.percentage), value: Number(o.userShare.value) },
        })),
        summary: r.summary.map(s => ({
          currency: s.currency,
          totalAmount: Number(s.totalAmount),
          userTotal: Number(s.userTotal),
          contactTotals: s.contactTotals.map(c => ({
            contactId: c.contactId,
            amount: Number(c.amount),
          })),
          byPaymentSource: s.byPaymentSource.map(ps => ({
            paymentSourceId: ps.paymentSourceId,
            totalAmount: Number(ps.totalAmount),
            userTotal: Number(ps.userTotal),
            contactTotals: ps.contactTotals.map(c => ({
              contactId: c.contactId,
              amount: Number(c.amount),
            })),
          })),
        })),
      } as PaymentOccurrencesResponse))
    );
  }

  getById(id: string): Observable<Payment> {
    return this.api.getPayment(id).pipe(
      map(p => ({
        ...p,
        currentAmount: Number(p.currentAmount),
        initialAmount: Number(p.initialAmount),
        values: (p.values ?? []).map((v: any) => ({ effectiveDate: v.effectiveDate, amount: Number(v.amount) })),
        userShare: { percentage: Number(p.userShare.percentage), value: Number(p.userShare.value) },
      } as Payment))
    );
  }

  create(req: CreatePaymentRequest): Observable<Payment> {
    return this.api.createPayment({
      ...req,
      endDate: req.endDate ?? null,
      splits: req.splits?.map(s => ({ contactId: s.contactId, percentage: s.percentage })) ?? null,
    }).pipe(
      map(p => ({
        ...p,
        currentAmount: Number(p.currentAmount),
        initialAmount: Number(p.initialAmount),
        values: (p.values ?? []).map((v: any) => ({ effectiveDate: v.effectiveDate, amount: Number(v.amount) })),
        userShare: { percentage: Number(p.userShare.percentage), value: Number(p.userShare.value) },
      } as Payment))
    );
  }

  update(id: string, req: UpdatePaymentRequest): Observable<Payment> {
    return this.api.updatePayment(id, {
      ...req,
      endDate: req.endDate ?? null,
      splits: req.splits?.map(s => ({ contactId: s.contactId, percentage: s.percentage })) ?? null,
    }).pipe(
      map(p => ({
        ...p,
        currentAmount: Number(p.currentAmount),
        initialAmount: Number(p.initialAmount),
        values: (p.values ?? []).map((v: any) => ({ effectiveDate: v.effectiveDate, amount: Number(v.amount) })),
        userShare: { percentage: Number(p.userShare.percentage), value: Number(p.userShare.value) },
      } as Payment))
    );
  }

  addValue(paymentId: string, req: AddPaymentValueRequest): Observable<{ paymentId: string; effectiveDate: string; amount: number }> {
    return this.api.addPaymentValue(paymentId, req) as Observable<any>;
  }

  removeValue(paymentId: string, effectiveDate: string): Observable<void> {
    return this.api.removePaymentValue(paymentId, effectiveDate);
  }

  delete(id: string): Observable<void> {
    return this.api.deletePayment(id) as Observable<void>;
  }
}
