import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import { CreatePersonRequest, Person, UpdatePersonRequest } from '../models/person.model';

@Service()
export class PersonService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(): Observable<Person[]> {
    return this.api.getAllPeople().pipe(map(r => r.people as Person[]));
  }

  create(req: CreatePersonRequest): Observable<Person> {
    return this.api.createPerson(req) as Observable<Person>;
  }

  update(id: string, req: UpdatePersonRequest): Observable<Person> {
    return this.api.updatePerson(id, req) as Observable<Person>;
  }

  delete(id: string): Observable<void> {
    return this.api.deletePerson(id) as Observable<void>;
  }
}
