import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import {
  AddPaymentValueRequest,
  CreatePaymentRequest,
  CurrencySummary,
  DirectionTotals,
  GroupSummary,
  NetTotals,
  OccurrenceSummaryPersonAmount,
  OccurrenceSummaryPaymentSourceBreakdown,
  OccurrenceSummaryPayeeAmount,
  Payment,
  PaymentOccurrence,
  PaymentOccurrencesResponse,
  PaymentSplit,
  PersonCommitment,
  UpdatePaymentRequest
} from '../models/payment.model';
import { PaymentDirection } from '../models/payment-direction.enum';

/** Common shape shared by every generated payment response (Create/Update/Get/GetAll item). */
interface ApiPaymentLike {
  id: string;
  userId: string;
  paymentSourceId: string;
  payeeId: string;
  currentAmount: unknown;
  initialAmount: unknown;
  values?: { effectiveDate: string; amount: unknown }[] | null;
  currency: string;
  frequency: number;
  direction: number;
  startDate: string;
  endDate?: string | null;
  description?: string | null;
  payerGroupId?: string | null;
  splits?: { personId: string; percentage: unknown; value: unknown }[] | null;
}

function toPayment(p: ApiPaymentLike): Payment {
  return {
    id: p.id,
    userId: p.userId,
    paymentSourceId: p.paymentSourceId,
    payeeId: p.payeeId,
    currentAmount: Number(p.currentAmount),
    initialAmount: Number(p.initialAmount),
    values: (p.values ?? []).map(v => ({ effectiveDate: v.effectiveDate, amount: Number(v.amount) })),
    currency: p.currency,
    frequency: p.frequency,
    direction: p.direction,
    startDate: p.startDate,
    endDate: p.endDate ?? undefined,
    description: p.description ?? undefined,
    payerGroupId: p.payerGroupId ?? null,
    splits: toSplits(p.splits),
  };
}

function toSplits(splits?: { personId: string; percentage: unknown; value?: unknown }[] | null): PaymentSplit[] {
  return (splits ?? []).map(s => ({ personId: s.personId, percentage: Number(s.percentage), value: s.value === undefined ? undefined : Number(s.value) }));
}

function toPersonTotals(totals: { personId: string; amount: unknown }[]): OccurrenceSummaryPersonAmount[] {
  return totals.map(p => ({ personId: p.personId, amount: Number(p.amount) }));
}

function toPayeeTotals(totals: { payeeId: string; amount: unknown }[]): OccurrenceSummaryPayeeAmount[] {
  return totals.map(p => ({ payeeId: p.payeeId, amount: Number(p.amount) }));
}

function toPaymentSourceBreakdown(
  breakdown: { paymentSourceId: string; totalAmount: unknown; personTotals: { personId: string; amount: unknown }[] }[]
): OccurrenceSummaryPaymentSourceBreakdown[] {
  return breakdown.map(ps => ({
    paymentSourceId: ps.paymentSourceId,
    totalAmount: Number(ps.totalAmount),
    personTotals: toPersonTotals(ps.personTotals),
  }));
}

function toDirectionTotals(d: {
  totalAmount: unknown;
  personTotals: { personId: string; amount: unknown }[];
  byPaymentSource: { paymentSourceId: string; totalAmount: unknown; personTotals: { personId: string; amount: unknown }[] }[];
}): DirectionTotals {
  return {
    totalAmount: Number(d.totalAmount),
    personTotals: toPersonTotals(d.personTotals),
    byPaymentSource: toPaymentSourceBreakdown(d.byPaymentSource),
  };
}

function toNetTotals(n: { totalAmount: unknown; personTotals: { personId: string; amount: unknown }[] }): NetTotals {
  return {
    totalAmount: Number(n.totalAmount),
    personTotals: toPersonTotals(n.personTotals),
  };
}

function toPersonCommitment(p: {
  personId: string;
  currency: string;
  income: unknown;
  committed: unknown;
  remaining: unknown;
  isOverCommitted: boolean;
}): PersonCommitment {
  return {
    personId: p.personId,
    currency: p.currency,
    income: Number(p.income),
    committed: Number(p.committed),
    remaining: Number(p.remaining),
    isOverCommitted: p.isOverCommitted,
  };
}

@Service()
export class PaymentService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(direction?: PaymentDirection): Observable<Payment[]> {
    return this.api.getAllPayments(direction).pipe(
      map(r => r.payments.map(toPayment))
    );
  }

  getOccurrences(from: string, to: string): Observable<PaymentOccurrencesResponse> {
    return this.api.getPaymentOccurrences(from, to).pipe(
      map(r => ({
        occurrences: r.occurrences.map((o): PaymentOccurrence => ({
          paymentId: o.paymentId,
          paymentSourceId: o.paymentSourceId,
          payeeId: o.payeeId,
          amount: Number(o.amount),
          currency: o.currency,
          frequency: o.frequency,
          direction: o.direction,
          occurrenceDate: o.occurrenceDate,
          startDate: o.startDate,
          endDate: o.endDate ?? undefined,
          description: o.description ?? undefined,
          payerGroupId: o.payerGroupId ?? null,
          splits: toSplits(o.splits),
        })),
        summary: r.summary.map((g): GroupSummary => ({
          payerGroupId: g.payerGroupId,
          currencies: g.currencies.map((c): CurrencySummary => ({
            currency: c.currency,
            outgoing: toDirectionTotals(c.outgoing),
            incoming: toDirectionTotals(c.incoming),
            net: toNetTotals(c.net),
            outgoingByPayee: toPayeeTotals(c.outgoingByPayee),
          })),
        })),
        people: r.people.map(toPersonCommitment),
      }))
    );
  }

  getById(id: string): Observable<Payment> {
    return this.api.getPayment(id).pipe(map(toPayment));
  }

  create(req: CreatePaymentRequest): Observable<Payment> {
    return this.api.createPayment({
      ...req,
      endDate: req.endDate ?? null,
      payerGroupId: req.payerGroupId ?? null,
      splits: req.splits?.map(s => ({ personId: s.personId, percentage: s.percentage })) ?? null,
    }).pipe(map(toPayment));
  }

  update(id: string, req: UpdatePaymentRequest): Observable<Payment> {
    return this.api.updatePayment(id, {
      ...req,
      endDate: req.endDate ?? null,
      payerGroupId: req.payerGroupId ?? null,
      splits: req.splits?.map(s => ({ personId: s.personId, percentage: s.percentage })) ?? null,
    }).pipe(map(toPayment));
  }

  addValue(paymentId: string, req: AddPaymentValueRequest): Observable<{ paymentId: string; effectiveDate: string; amount: number }> {
    return this.api.addPaymentValue(paymentId, req).pipe(
      map(r => ({ paymentId: r.paymentId, effectiveDate: r.effectiveDate, amount: Number(r.amount) }))
    );
  }

  removeValue(paymentId: string, effectiveDate: string): Observable<void> {
    return this.api.removePaymentValue(paymentId, effectiveDate);
  }

  delete(id: string): Observable<void> {
    return this.api.deletePayment(id) as Observable<void>;
  }
}
