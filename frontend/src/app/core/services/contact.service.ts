import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PaymentManagerWebApiService } from '../../../api-client';
import { Contact, CreateContactRequest, UpdateContactRequest } from '../models/contact.model';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly api = inject(PaymentManagerWebApiService);

  getAll(): Observable<Contact[]> {
    return this.api.getAllContacts().pipe(map(r => r.contacts as Contact[]));
  }

  create(req: CreateContactRequest): Observable<Contact> {
    return this.api.createContact(req) as Observable<Contact>;
  }

  update(id: string, req: UpdateContactRequest): Observable<Contact> {
    return this.api.updateContact(id, req) as Observable<Contact>;
  }

  delete(id: string): Observable<void> {
    return this.api.deleteContact(id) as Observable<void>;
  }
}
