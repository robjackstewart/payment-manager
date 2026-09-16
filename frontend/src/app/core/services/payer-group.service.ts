import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import {
  CreatePayerGroupRequest,
  PayerGroup,
  SetPayerGroupMembersRequest,
  UpdatePayerGroupRequest,
} from '../models/payer-group.model';

@Injectable({ providedIn: 'root' })
export class PayerGroupService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(): Observable<PayerGroup[]> {
    return this.api.getAllPayerGroups().pipe(map(r => r.payerGroups as PayerGroup[]));
  }

  create(req: CreatePayerGroupRequest): Observable<PayerGroup> {
    return this.api.createPayerGroup(req) as Observable<PayerGroup>;
  }

  update(id: string, req: UpdatePayerGroupRequest): Observable<PayerGroup> {
    return this.api.updatePayerGroup(id, req) as Observable<PayerGroup>;
  }

  delete(id: string): Observable<void> {
    return this.api.deletePayerGroup(id) as Observable<void>;
  }

  setMembers(id: string, req: SetPayerGroupMembersRequest): Observable<{ payerGroupId: string; personIds: string[] }> {
    return this.api.setPayerGroupMembers(id, req) as Observable<{ payerGroupId: string; personIds: string[] }>;
  }
}
