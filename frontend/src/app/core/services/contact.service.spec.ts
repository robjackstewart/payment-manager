import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { PaymentManagerWebApiService } from '../../../api-client';
import { ContactService } from './contact.service';

function setup() {
  const apiMock = {
    getAllContacts: vi.fn(),
    createContact: vi.fn(),
    updateContact: vi.fn(),
    deleteContact: vi.fn(),
  };
  TestBed.configureTestingModule({
    providers: [
      ContactService,
      { provide: PaymentManagerWebApiService, useValue: apiMock },
    ],
  });
  const service = TestBed.inject(ContactService);
  return { service, apiMock };
}

describe('ContactService', () => {
  describe('getAll()', () => {
    it('maps the contacts array from the API response', async () => {
      const { service, apiMock } = setup();
      const contacts = [{ id: '1', name: 'Alice' }];
      apiMock.getAllContacts.mockReturnValue(of({ contacts }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual(contacts);
    });

    it('returns an empty array when contacts is empty', async () => {
      const { service, apiMock } = setup();
      apiMock.getAllContacts.mockReturnValue(of({ contacts: [] }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual([]);
    });
  });

  describe('create()', () => {
    it('calls createContact with the request and returns the created contact', async () => {
      const { service, apiMock } = setup();
      const req = { name: 'Alice' };
      const created = { id: '1', name: 'Alice' };
      apiMock.createContact.mockReturnValue(of(created));

      const result = await firstValueFrom(service.create(req));

      expect(apiMock.createContact).toHaveBeenCalledWith(req);
      expect(result).toEqual(created);
    });
  });

  describe('update()', () => {
    it('calls updateContact with the id and request', async () => {
      const { service, apiMock } = setup();
      const updated = { id: '1', name: 'Bob' };
      apiMock.updateContact.mockReturnValue(of(updated));

      await firstValueFrom(service.update('1', { name: 'Bob' }));

      expect(apiMock.updateContact).toHaveBeenCalledWith('1', { name: 'Bob' });
    });
  });

  describe('delete()', () => {
    it('calls deleteContact with the id', async () => {
      const { service, apiMock } = setup();
      apiMock.deleteContact.mockReturnValue(of(undefined));

      await firstValueFrom(service.delete('1'));

      expect(apiMock.deleteContact).toHaveBeenCalledWith('1');
    });
  });
});
