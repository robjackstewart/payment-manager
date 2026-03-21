import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import {
  CreatePaymentSourceRequest,
  PaymentSource,
  UpdatePaymentSourceRequest
} from '../models/payment-source.model';

@Injectable({ providedIn: 'root' })
export class PaymentSourceService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(userId: string): Observable<PaymentSource[]> {
    return this.api.getAllPaymentSources(userId).pipe(map(r => r.paymentSources as PaymentSource[]));
  }

  getById(id: string): Observable<PaymentSource> {
    return this.api.getPaymentSource(id) as Observable<PaymentSource>;
  }

  create(req: CreatePaymentSourceRequest): Observable<PaymentSource> {
    return this.api.createPaymentSource(req) as Observable<PaymentSource>;
  }

  update(id: string, req: UpdatePaymentSourceRequest): Observable<PaymentSource> {
    return this.api.updatePaymentSource(id, req) as Observable<PaymentSource>;
  }

  delete(id: string): Observable<void> {
    return this.api.deletePaymentSource(id) as Observable<void>;
  }
}
