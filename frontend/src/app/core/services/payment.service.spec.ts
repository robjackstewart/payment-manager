import { TestBed } from '@angular/core/testing';
import { of, firstValueFrom } from 'rxjs';
import { vi, describe, it, expect } from 'vitest';
import { PaymentManagerWebApiService } from '../../../api-client';
import { PaymentFrequency } from '../models/payment-frequency.enum';
import { PaymentSplit } from '../models/payment.model';
import { PaymentService } from './payment.service';

const mockPaymentDto = {
  id: '1',
  paymentSourceId: 'ps1',
  payeeId: 'py1',
  currency: 'USD',
  frequency: 'Monthly',
  startDate: '2024-01-01',
  endDate: null,
  description: null,
  currentAmount: '100.50',
  initialAmount: '100.50',
  values: [{ effectiveDate: '2024-01-01', amount: '100.50' }],
  userShare: { percentage: '50', value: '50.25' },
  splits: [],
};

function setup() {
  const apiSpy = {
    getAllPayments: vi.fn(),
    getPayment: vi.fn(),
    getPaymentOccurrences: vi.fn(),
    createPayment: vi.fn(),
    updatePayment: vi.fn(),
    addPaymentValue: vi.fn(),
    removePaymentValue: vi.fn(),
    deletePayment: vi.fn(),
  };

  TestBed.configureTestingModule({
    providers: [
      PaymentService,
      { provide: PaymentManagerWebApiService, useValue: apiSpy },
    ],
  });

  const service = TestBed.inject(PaymentService);
  return { service, apiSpy };
}

describe('PaymentService', () => {
  describe('getAll()', () => {
    it('converts string amounts to numbers', async () => {
      const { service, apiSpy } = setup();
      apiSpy['getAllPayments'].mockReturnValue(of({ payments: [mockPaymentDto] }));

      const [payment] = await firstValueFrom(service.getAll());

      expect(typeof payment.currentAmount).toBe('number');
      expect(payment.currentAmount).toBe(100.5);
      expect(typeof payment.initialAmount).toBe('number');
      expect(payment.initialAmount).toBe(100.5);
      expect(typeof payment.values[0].amount).toBe('number');
      expect(payment.values[0].amount).toBe(100.5);
      expect(typeof payment.userShare.percentage).toBe('number');
      expect(payment.userShare.percentage).toBe(50);
      expect(typeof payment.userShare.value).toBe('number');
      expect(payment.userShare.value).toBe(50.25);
    });

    it('returns an empty array when there are no payments', async () => {
      const { service, apiSpy } = setup();
      apiSpy['getAllPayments'].mockReturnValue(of({ payments: [] }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual([]);
    });
  });

  describe('getById(id)', () => {
    it('passes the correct id to the API', async () => {
      const { service, apiSpy } = setup();
      apiSpy['getPayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(service.getById('42'));

      expect(apiSpy['getPayment']).toHaveBeenCalledWith('42');
    });

    it('converts string amounts to numbers', async () => {
      const { service, apiSpy } = setup();
      apiSpy['getPayment'].mockReturnValue(of(mockPaymentDto));

      const payment = await firstValueFrom(service.getById('1'));

      expect(typeof payment.currentAmount).toBe('number');
      expect(payment.currentAmount).toBe(100.5);
      expect(typeof payment.initialAmount).toBe('number');
      expect(payment.initialAmount).toBe(100.5);
      expect(typeof payment.values[0].amount).toBe('number');
      expect(payment.values[0].amount).toBe(100.5);
      expect(typeof payment.userShare.percentage).toBe('number');
      expect(payment.userShare.percentage).toBe(50);
      expect(typeof payment.userShare.value).toBe('number');
      expect(payment.userShare.value).toBe(50.25);
    });
  });

  describe('getOccurrences(from, to)', () => {
    const mockOccurrenceDto = {
      paymentId: '1',
      paymentSourceId: 'ps1',
      payeeId: 'py1',
      amount: '200.00',
      currency: 'USD',
      frequency: 'Monthly',
      occurrenceDate: '2024-03-01',
      startDate: '2024-01-01',
      userShare: { percentage: '50', value: '100.00' },
      splits: [],
    };

    const mockSummaryDto = {
      currency: 'USD',
      totalAmount: '400.00',
      userTotal: '200.00',
      contactTotals: [{ contactId: 'c1', amount: '200.00' }],
      byPaymentSource: [
        {
          paymentSourceId: 'ps1',
          totalAmount: '400.00',
          userTotal: '200.00',
          contactTotals: [{ contactId: 'c1', amount: '200.00' }],
        },
      ],
    };

    it('passes from and to strings to the API', async () => {
      const { service, apiSpy } = setup();
      apiSpy['getPaymentOccurrences'].mockReturnValue(
        of({ occurrences: [], summary: [] })
      );

      await firstValueFrom(service.getOccurrences('2024-01-01', '2024-03-31'));

      expect(apiSpy['getPaymentOccurrences']).toHaveBeenCalledWith(
        '2024-01-01',
        '2024-03-31'
      );
    });

    it('converts occurrence string amounts to numbers', async () => {
      const { service, apiSpy } = setup();
      apiSpy['getPaymentOccurrences'].mockReturnValue(
        of({ occurrences: [mockOccurrenceDto], summary: [] })
      );

      const result = await firstValueFrom(
        service.getOccurrences('2024-01-01', '2024-03-31')
      );
      const occ = result.occurrences[0];

      expect(typeof occ.amount).toBe('number');
      expect(occ.amount).toBe(200);
      expect(typeof occ.userShare.percentage).toBe('number');
      expect(occ.userShare.percentage).toBe(50);
      expect(typeof occ.userShare.value).toBe('number');
      expect(occ.userShare.value).toBe(100);
    });

    it('converts summary string amounts to numbers', async () => {
      const { service, apiSpy } = setup();
      apiSpy['getPaymentOccurrences'].mockReturnValue(
        of({ occurrences: [], summary: [mockSummaryDto] })
      );

      const result = await firstValueFrom(
        service.getOccurrences('2024-01-01', '2024-03-31')
      );
      const summary = result.summary[0];

      expect(typeof summary.totalAmount).toBe('number');
      expect(summary.totalAmount).toBe(400);
      expect(typeof summary.userTotal).toBe('number');
      expect(summary.userTotal).toBe(200);
      expect(typeof summary.contactTotals[0].amount).toBe('number');
      expect(summary.contactTotals[0].amount).toBe(200);

      const ps = summary.byPaymentSource[0];
      expect(typeof ps.totalAmount).toBe('number');
      expect(ps.totalAmount).toBe(400);
      expect(typeof ps.userTotal).toBe('number');
      expect(ps.userTotal).toBe(200);
      expect(typeof ps.contactTotals[0].amount).toBe('number');
      expect(ps.contactTotals[0].amount).toBe(200);
    });
  });

  describe('create(req)', () => {
    const baseReq = {
      paymentSourceId: 'ps1',
      payeeId: 'py1',
      amount: 100.5,
      currency: 'USD',
      frequency: PaymentFrequency.Monthly,
      startDate: '2024-01-01',
    };

    it('passes endDate as null when undefined', async () => {
      const { service, apiSpy } = setup();
      apiSpy['createPayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(service.create({ ...baseReq, endDate: undefined }));

      expect(apiSpy['createPayment']).toHaveBeenCalledWith(
        expect.objectContaining({ endDate: null })
      );
    });

    it('passes endDate through when provided', async () => {
      const { service, apiSpy } = setup();
      apiSpy['createPayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(
        service.create({ ...baseReq, endDate: '2025-12-31' })
      );

      expect(apiSpy['createPayment']).toHaveBeenCalledWith(
        expect.objectContaining({ endDate: '2025-12-31' })
      );
    });

    it('passes splits as null when undefined', async () => {
      const { service, apiSpy } = setup();
      apiSpy['createPayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(service.create({ ...baseReq, splits: undefined }));

      expect(apiSpy['createPayment']).toHaveBeenCalledWith(
        expect.objectContaining({ splits: null })
      );
    });

    it('passes splits as null when null', async () => {
      const { service, apiSpy } = setup();
      apiSpy['createPayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(service.create({ ...baseReq, splits: null as unknown as PaymentSplit[] }));

      expect(apiSpy['createPayment']).toHaveBeenCalledWith(
        expect.objectContaining({ splits: null })
      );
    });

    it('converts response string amounts to numbers', async () => {
      const { service, apiSpy } = setup();
      apiSpy['createPayment'].mockReturnValue(of(mockPaymentDto));

      const payment = await firstValueFrom(service.create(baseReq));

      expect(typeof payment.currentAmount).toBe('number');
      expect(payment.currentAmount).toBe(100.5);
      expect(typeof payment.initialAmount).toBe('number');
      expect(payment.initialAmount).toBe(100.5);
      expect(typeof payment.userShare.percentage).toBe('number');
      expect(payment.userShare.percentage).toBe(50);
      expect(typeof payment.userShare.value).toBe('number');
      expect(payment.userShare.value).toBe(50.25);
    });
  });

  describe('update(id, req)', () => {
    const baseReq = {
      paymentSourceId: 'ps1',
      payeeId: 'py1',
      initialAmount: 100.5,
      currency: 'USD',
      frequency: PaymentFrequency.Monthly,
      startDate: '2024-01-01',
    };

    it('passes the correct id to the API', async () => {
      const { service, apiSpy } = setup();
      apiSpy['updatePayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(service.update('99', baseReq));

      expect(apiSpy['updatePayment']).toHaveBeenCalledWith(
        '99',
        expect.any(Object)
      );
    });

    it('passes endDate as null when undefined', async () => {
      const { service, apiSpy } = setup();
      apiSpy['updatePayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(service.update('1', { ...baseReq, endDate: undefined }));

      expect(apiSpy['updatePayment']).toHaveBeenCalledWith(
        '1',
        expect.objectContaining({ endDate: null })
      );
    });

    it('passes endDate through when provided', async () => {
      const { service, apiSpy } = setup();
      apiSpy['updatePayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(
        service.update('1', { ...baseReq, endDate: '2025-12-31' })
      );

      expect(apiSpy['updatePayment']).toHaveBeenCalledWith(
        '1',
        expect.objectContaining({ endDate: '2025-12-31' })
      );
    });

    it('passes splits as null when undefined', async () => {
      const { service, apiSpy } = setup();
      apiSpy['updatePayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(service.update('1', { ...baseReq, splits: undefined }));

      expect(apiSpy['updatePayment']).toHaveBeenCalledWith(
        '1',
        expect.objectContaining({ splits: null })
      );
    });

    it('passes splits as null when null', async () => {
      const { service, apiSpy } = setup();
      apiSpy['updatePayment'].mockReturnValue(of(mockPaymentDto));

      await firstValueFrom(
        service.update('1', { ...baseReq, splits: null as unknown as PaymentSplit[] })
      );

      expect(apiSpy['updatePayment']).toHaveBeenCalledWith(
        '1',
        expect.objectContaining({ splits: null })
      );
    });

    it('converts response string amounts to numbers', async () => {
      const { service, apiSpy } = setup();
      apiSpy['updatePayment'].mockReturnValue(of(mockPaymentDto));

      const payment = await firstValueFrom(service.update('1', baseReq));

      expect(typeof payment.currentAmount).toBe('number');
      expect(payment.currentAmount).toBe(100.5);
      expect(typeof payment.userShare.percentage).toBe('number');
      expect(payment.userShare.percentage).toBe(50);
    });
  });

  describe('addValue(paymentId, req)', () => {
    it('delegates directly to the API and passes through the response', async () => {
      const { service, apiSpy } = setup();
      const mockResponse = { paymentId: '1', effectiveDate: '2024-06-01', amount: 120 };
      apiSpy['addPaymentValue'].mockReturnValue(of(mockResponse));

      const result = await firstValueFrom(
        service.addValue('1', { effectiveDate: '2024-06-01', amount: 120 })
      );

      expect(apiSpy['addPaymentValue']).toHaveBeenCalledWith('1', {
        effectiveDate: '2024-06-01',
        amount: 120,
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe('removeValue(paymentId, effectiveDate)', () => {
    it('delegates directly to the API', async () => {
      const { service, apiSpy } = setup();
      apiSpy['removePaymentValue'].mockReturnValue(of(undefined));

      await firstValueFrom(service.removeValue('1', '2024-06-01'));

      expect(apiSpy['removePaymentValue']).toHaveBeenCalledWith('1', '2024-06-01');
    });
  });

  describe('delete(id)', () => {
    it('passes the correct id to the API', async () => {
      const { service, apiSpy } = setup();
      apiSpy['deletePayment'].mockReturnValue(of(undefined));

      await firstValueFrom(service.delete('7'));

      expect(apiSpy['deletePayment']).toHaveBeenCalledWith('7');
    });
  });
});
