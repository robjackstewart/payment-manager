import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/dashboard/dashboard').then(m => m.DashboardComponent)
  },
  {
    path: 'users',
    loadComponent: () =>
      import('./features/users/user-list/user-list').then(m => m.UserListComponent)
  },
  {
    path: 'payment-sources',
    loadComponent: () =>
      import('./features/payment-sources/payment-source-list/payment-source-list').then(
        m => m.PaymentSourceListComponent
      )
  },
  {
    path: 'payees',
    loadComponent: () =>
      import('./features/payees/payee-list/payee-list').then(m => m.PayeeListComponent)
  },
  {
    path: 'payments',
    loadComponent: () =>
      import('./features/payments/payment-list/payment-list').then(m => m.PaymentListComponent)
  },
  { path: '**', redirectTo: '' }
];
