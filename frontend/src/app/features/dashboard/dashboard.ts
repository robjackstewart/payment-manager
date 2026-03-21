import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from '../../core/services/user.service';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentService } from '../../core/services/payment.service';
import { UserContextService } from '../../core/services/user-context.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent {
  private readonly userService = inject(UserService);
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly paymentService = inject(PaymentService);
  readonly userContext = inject(UserContextService);

  readonly loading = signal(false);
  readonly userCount = signal(0);
  readonly paymentSourceCount = signal(0);
  readonly payeeCount = signal(0);
  readonly paymentCount = signal(0);

  constructor() {
    this.userService.getAll().subscribe(users => this.userCount.set(users.length));

    effect(() => {
      const user = this.userContext.selectedUser();
      if (!user) {
        this.paymentSourceCount.set(0);
        this.payeeCount.set(0);
        this.paymentCount.set(0);
        return;
      }
      this.loading.set(true);
      forkJoin({
        paymentSources: this.paymentSourceService.getAll(user.id),
        payees: this.payeeService.getAll(user.id),
        payments: this.paymentService.getAll(user.id),
      }).subscribe({
        next: ({ paymentSources, payees, payments }) => {
          this.paymentSourceCount.set(paymentSources.length);
          this.payeeCount.set(payees.length);
          this.paymentCount.set(payments.length);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    });
  }
}
