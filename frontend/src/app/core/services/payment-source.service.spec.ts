import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { PaymentManagerWebApiService } from '../../../api-client';
import { PaymentSource } from '../models/payment-source.model';
import { PaymentSourceService } from './payment-source.service';

const mockSource: PaymentSource = { id: '1', userId: 'u1', name: 'Wallet' };

function makeApi() {
  return {
    getAllPaymentSources: vi.fn(),
    getPaymentSource: vi.fn(),
    createPaymentSource: vi.fn(),
    updatePaymentSource: vi.fn(),
    deletePaymentSource: vi.fn(),
  };
}

function setup() {
  const api = makeApi();

  TestBed.configureTestingModule({
    providers: [
      PaymentSourceService,
      { provide: PaymentManagerWebApiService, useValue: api },
    ],
  });

  const service = TestBed.inject(PaymentSourceService);
  return { service, api };
}

describe('PaymentSourceService', () => {
  describe('getAll()', () => {
    it('maps paymentSources from the API response', async () => {
      const { service, api } = setup();
      api.getAllPaymentSources.mockReturnValue(of({ paymentSources: [mockSource] }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual([mockSource]);
    });

    it('returns an empty array when paymentSources is empty', async () => {
      const { service, api } = setup();
      api.getAllPaymentSources.mockReturnValue(of({ paymentSources: [] }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual([]);
    });
  });

  describe('getById(id)', () => {
    it('calls getPaymentSource with the given id', async () => {
      const { service, api } = setup();
      api.getPaymentSource.mockReturnValue(of(mockSource));

      const result = await firstValueFrom(service.getById('1'));

      expect(api.getPaymentSource).toHaveBeenCalledWith('1');
      expect(result).toEqual(mockSource);
    });
  });

  describe('create(req)', () => {
    it('passes the request to createPaymentSource and returns the result', async () => {
      const { service, api } = setup();
      const req = { name: 'New Card' };
      api.createPaymentSource.mockReturnValue(of(mockSource));

      const result = await firstValueFrom(service.create(req));

      expect(api.createPaymentSource).toHaveBeenCalledWith(req);
      expect(result).toEqual(mockSource);
    });
  });

  describe('update(id, req)', () => {
    it('passes both id and request to updatePaymentSource and returns the result', async () => {
      const { service, api } = setup();
      const req = { name: 'Updated' };
      api.updatePaymentSource.mockReturnValue(of(mockSource));

      const result = await firstValueFrom(service.update('1', req));

      expect(api.updatePaymentSource).toHaveBeenCalledWith('1', req);
      expect(result).toEqual(mockSource);
    });
  });

  describe('delete(id)', () => {
    it('passes the id to deletePaymentSource', async () => {
      const { service, api } = setup();
      api.deletePaymentSource.mockReturnValue(of(undefined));

      await firstValueFrom(service.delete('1'));

      expect(api.deletePaymentSource).toHaveBeenCalledWith('1');
    });
  });
});
