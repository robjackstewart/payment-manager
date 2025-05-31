import { inject, Component, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PaymentManagerWebApiService } from '../../../payment-manager-api-client';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'payment-manager';

  private readonly paymentManagerWebApiService = inject(PaymentManagerWebApiService);

  public readonly users = computed(() => this.getAllUsersResult()?.value || []);
  public readonly errorMessage = computed(() => this.getAllUsersResult()?.error);
  public readonly isLoading = computed(() => this.getAllUsersResult().isLoading);

  constructor() {
    console.log(this.paymentManagerWebApiService);
  }

  private readonly getAllUsersResult = toSignal(
    this.paymentManagerWebApiService.getAllUsers().pipe(
      map((response) => ({
        value: response.users,
        error: null,
        isLoading: false
      })),
      catchError((error) => {
        return of({
          value: null,
          error: error.message || 'An unknown error occurred',
          isLoading: false
        });
      })
    ),
    {
      initialValue: { value: null, error: null, isLoading: true },
    }
  );
}
