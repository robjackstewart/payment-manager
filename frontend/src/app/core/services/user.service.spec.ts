import { TestBed } from '@angular/core/testing';
import { of, firstValueFrom } from 'rxjs';
import { PaymentManagerWebApiService } from '../../../api-client';
import { UserService } from './user.service';
import type { User, CreateUserRequest, UpdateUserRequest } from '../models/user.model';

const mockUser = (overrides?: Partial<User>): User => ({
  id: 'user-1',
  name: 'Alice',
  ...overrides,
});

const mockApi = () => ({
  getAllUsers: vi.fn(),
  getUser: vi.fn(),
  createUser: vi.fn(),
  updateUser: vi.fn(),
  deleteUser: vi.fn(),
});

describe('UserService', () => {
  const setup = () => {
    const api = mockApi();

    TestBed.configureTestingModule({
      providers: [
        UserService,
        { provide: PaymentManagerWebApiService, useValue: api },
      ],
    });

    const service = TestBed.inject(UserService);
    return { service, api };
  };

  describe('getAll()', () => {
    it('maps r.users from the API response', async () => {
      const { service, api } = setup();
      const users = [mockUser(), mockUser({ id: 'user-2', name: 'Bob' })];
      api.getAllUsers.mockReturnValue(of({ users }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual(users);
    });

    it('returns [] when the API response contains an empty users array', async () => {
      const { service, api } = setup();
      api.getAllUsers.mockReturnValue(of({ users: [] }));

      const result = await firstValueFrom(service.getAll());

      expect(result).toEqual([]);
    });
  });

  describe('getById()', () => {
    it('calls api.getUser with the correct id and returns the result', async () => {
      const { service, api } = setup();
      const user = mockUser({ id: 'user-42' });
      api.getUser.mockReturnValue(of(user));

      const result = await firstValueFrom(service.getById('user-42'));

      expect(api.getUser).toHaveBeenCalledWith('user-42');
      expect(result).toEqual(user);
    });
  });

  describe('create()', () => {
    it('calls api.createUser with the correct request and returns the result', async () => {
      const { service, api } = setup();
      const req: CreateUserRequest = { name: 'Charlie' };
      const created = mockUser({ id: 'user-new', name: 'Charlie' });
      api.createUser.mockReturnValue(of(created));

      const result = await firstValueFrom(service.create(req));

      expect(api.createUser).toHaveBeenCalledWith(req);
      expect(result).toEqual(created);
    });
  });

  describe('update()', () => {
    it('calls api.updateUser with the correct id and request and returns the result', async () => {
      const { service, api } = setup();
      const req: UpdateUserRequest = { name: 'Alice Updated' };
      const updated = mockUser({ name: 'Alice Updated' });
      api.updateUser.mockReturnValue(of(updated));

      const result = await firstValueFrom(service.update('user-1', req));

      expect(api.updateUser).toHaveBeenCalledWith('user-1', req);
      expect(result).toEqual(updated);
    });
  });

  describe('delete()', () => {
    it('calls api.deleteUser with the correct id', async () => {
      const { service, api } = setup();
      api.deleteUser.mockReturnValue(of(undefined));

      await firstValueFrom(service.delete('user-1'));

      expect(api.deleteUser).toHaveBeenCalledWith('user-1');
    });
  });
});
