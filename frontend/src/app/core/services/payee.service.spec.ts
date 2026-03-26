import { TestBed } from '@angular/core/testing';
import { of, firstValueFrom } from 'rxjs';
import { vi, describe, it, expect } from 'vitest';
import { PaymentManagerWebApiService } from '../../../api-client';
import { PayeeService } from './payee.service';
import type { Payee, CreatePayeeRequest, UpdatePayeeRequest } from '../models/payee.model';

const mockPayee = (overrides?: Partial<Payee>): Payee => ({
  id: 'payee-1',
  userId: 'user-1',
  name: 'Alice',
  ...overrides,
});

const mockApi = () => ({
  getAllPayees: vi.fn(),
  getPayee: vi.fn(),
  createPayee: vi.fn(),
  updatePayee: vi.fn(),
  deletePayee: vi.fn(),
});

describe('PayeeService', () => {
  const setup = () => {
    const api = mockApi();

    TestBed.configureTestingModule({
      providers: [
        PayeeService,
        { provide: PaymentManagerWebApiService, useValue: api },
      ],
    });

    const service = TestBed.inject(PayeeService);
    return { service, api };
  };

  describe('getAll()', () => {
    it('maps r.payees from the API response', async () => {
      const { service, api } = setup();
      const payees = [mockPayee(), mockPayee({ id: 'payee-2', name: 'Bob' })];
      api.getAllPayees.mockReturnValue(of({ payees }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual(payees);
    });

    it('returns [] when the API response contains an empty payees array', async () => {
      const { service, api } = setup();
      api.getAllPayees.mockReturnValue(of({ payees: [] }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual([]);
    });
  });

  describe('getById()', () => {
    it('calls api.getPayee with the correct id and returns the result', async () => {
      const { service, api } = setup();
      const payee = mockPayee({ id: 'payee-42' });
      api.getPayee.mockReturnValue(of(payee));

      const result = await firstValueFrom(service.getById('payee-42'));

      expect(api.getPayee).toHaveBeenCalledWith('payee-42');
      expect(result).toEqual(payee);
    });
  });

  describe('create()', () => {
    it('calls api.createPayee with the correct request and returns the result', async () => {
      const { service, api } = setup();
      const req: CreatePayeeRequest = { name: 'Charlie' };
      const created = mockPayee({ id: 'payee-new', name: 'Charlie' });
      api.createPayee.mockReturnValue(of(created));

      const result = await firstValueFrom(service.create(req));

      expect(api.createPayee).toHaveBeenCalledWith(req);
      expect(result).toEqual(created);
    });
  });

  describe('update()', () => {
    it('calls api.updatePayee with the correct id and request and returns the result', async () => {
      const { service, api } = setup();
      const req: UpdatePayeeRequest = { name: 'Alice Updated' };
      const updated = mockPayee({ name: 'Alice Updated' });
      api.updatePayee.mockReturnValue(of(updated));

      const result = await firstValueFrom(service.update('payee-1', req));

      expect(api.updatePayee).toHaveBeenCalledWith('payee-1', req);
      expect(result).toEqual(updated);
    });
  });

  describe('delete()', () => {
    it('calls api.deletePayee with the correct id', async () => {
      const { service, api } = setup();
      api.deletePayee.mockReturnValue(of(undefined));

      await firstValueFrom(service.delete('payee-1'));

      expect(api.deletePayee).toHaveBeenCalledWith('payee-1');
    });
  });
});
