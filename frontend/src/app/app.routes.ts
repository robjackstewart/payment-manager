import { Routes } from '@angular/router';
import { PaymentDirection } from './core/models/payment-direction.enum';

export const routes: Routes = [
  {
    path: '',
    title: 'Dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard').then(m => m.DashboardComponent)
  },
  {
    path: 'payment-sources',
    title: 'Payment Sources',
    loadComponent: () =>
      import('./features/payment-sources/payment-source-list/payment-source-list').then(
        m => m.PaymentSourceListComponent
      )
  },
  {
    path: 'payees',
    title: 'Payees',
    loadComponent: () =>
      import('./features/payees/payee-list/payee-list').then(m => m.PayeeListComponent)
  },
  {
    path: 'payments',
    title: 'Payments',
    data: { direction: PaymentDirection.Outgoing },
    loadComponent: () =>
      import('./features/payments/payment-list/payment-list').then(m => m.PaymentListComponent)
  },
  {
    path: 'income',
    title: 'Income',
    data: { direction: PaymentDirection.Incoming },
    loadComponent: () =>
      import('./features/payments/payment-list/payment-list').then(m => m.PaymentListComponent)
  },
  {
    path: 'people',
    title: 'People',
    loadComponent: () =>
      import('./features/people/person-list/person-list').then(m => m.PersonListComponent)
  },
  {
    path: 'payer-groups',
    title: 'Payer Groups',
    loadComponent: () =>
      import('./features/payer-groups/payer-group-list/payer-group-list').then(m => m.PayerGroupListComponent)
  },
  { path: '**', redirectTo: '' }
];
