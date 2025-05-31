import { Routes } from '@angular/router';
import { HomePageComponent } from './pages/home/home.page.component';
import { PaymentsPageComponent } from './pages/payments/payments.page.component';

export const routes: Routes = [
    {
        path: 'home',
        component: HomePageComponent
    },
    {
        path: 'payments',
        component: PaymentsPageComponent
    },
    {
        path: '',
        redirectTo: 'home',
        pathMatch: 'full'
    },
];
