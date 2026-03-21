import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PaymentSourceService } from '../../core/services/payment-source.service';
import { PayeeService } from '../../core/services/payee.service';
import { PaymentService } from '../../core/services/payment.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  private readonly paymentSourceService = inject(PaymentSourceService);
  private readonly payeeService = inject(PayeeService);
  private readonly paymentService = inject(PaymentService);

  readonly loading = signal(false);
  readonly paymentSourceCount = signal(0);
  readonly payeeCount = signal(0);
  readonly paymentCount = signal(0);

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      paymentSources: this.paymentSourceService.getAll(),
      payees: this.payeeService.getAll(),
      payments: this.paymentService.getAll(),
    }).subscribe({
      next: ({ paymentSources, payees, payments }) => {
        this.paymentSourceCount.set(paymentSources.length);
        this.payeeCount.set(payees.length);
        this.paymentCount.set(payments.length);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
