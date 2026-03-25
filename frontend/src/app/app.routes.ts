import { Routes } from '@angular/router';

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
    loadComponent: () =>
      import('./features/payments/payment-list/payment-list').then(m => m.PaymentListComponent)
  },
  {
    path: 'contacts',
    title: 'Contacts',
    loadComponent: () =>
      import('./features/contacts/contact-list/contact-list').then(m => m.ContactListComponent)
  },
  { path: '**', redirectTo: '' }
];
