---
applyTo: frontend/**
---

# Frontend Coding Instructions

## Framework

Angular 21.x with Angular Material. TypeScript strict mode is enabled.

## Signals First

Prefer Angular **signals** over RxJS observables and subscriptions wherever possible.

```typescript
// ✅ Prefer signals for component state
export class PaymentsComponent {
  payments = signal<Payment[]>([]);
  isLoading = signal(false);
  selectedPayment = signal<Payment | null>(null);

  filteredPayments = computed(() =>
    this.payments().filter(p => p.status === 'active')
  );
}

// ❌ Avoid subscriptions for local component state
export class PaymentsComponent implements OnInit, OnDestroy {
  payments: Payment[] = [];
  private subscription: Subscription;   // avoid

  ngOnInit() {
    this.subscription = this.service.getPayments().subscribe(...);
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }
}
```

### Guidelines
- Use `signal()` for mutable state, `computed()` for derived state, and `effect()` for side effects.
- Use `toSignal()` from `@angular/core/rxjs-interop` when you must consume an Observable (e.g. router events, HTTP responses from the api-client) — this bridges RxJS to signals cleanly.
- Use `input()` and `output()` signal-based APIs for component inputs and outputs instead of `@Input()` / `@Output()`.
- Template binding works natively with signals — call the signal as a function in templates: `{{ payments() }}`.
- Services that hold shared state should expose signals (or `readonly` signal views) rather than BehaviorSubjects.

```typescript
// ✅ Service exposing signal-based state
@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly _payments = signal<Payment[]>([]);
  readonly payments = this._payments.asReadonly();
}
```

## API Client

**All interaction with the backend Web API must go through the generated `api-client`.**

- Never make raw `HttpClient` calls to the backend API.
- Never hand-edit any file inside `src/api-client/` — it is fully generated and will be overwritten.
- Import and inject `PaymentManagerWebApiService` (or the relevant generated service) from `src/api-client/`.

```typescript
// ✅ Correct — use the generated api-client
import { PaymentManagerWebApiService } from '../api-client/api/payment-manager-web-api.service';

@Injectable({ providedIn: 'root' })
export class PaymentStore {
  private readonly api = inject(PaymentManagerWebApiService);

  loadPayments = () => toSignal(this.api.getPayments(), { initialValue: [] });
}

// ❌ Wrong — never call HttpClient directly for backend API calls
constructor(private http: HttpClient) {}
getPayments() { return this.http.get('/api/payments'); }
```

### Regenerating the API Client

When the backend OpenAPI spec changes (it is produced automatically during `dotnet build`), regenerate the client:

```bash
npm run api-client:generate
# or via task runner:
task frontend:api-client:generate
```

This reads the `openapi.json` produced by the backend build and regenerates all files under `src/api-client/`. Commit the regenerated files alongside any backend changes that affect the API contract.

## Project Structure

```
frontend/src/
  api-client/          # Generated — do not hand-edit
  app/
    core/
      services/        # Application-level services (wrap api-client, hold signal state)
    features/          # Feature modules (routed, self-contained)
    shared/            # Shared components, pipes, directives
  environments/
```

## Testing

**Framework:** Vitest + JSDOM

- Test files are colocated with source files (`*.spec.ts`).
- Use `TestBed` for component/service tests that need the Angular DI container.
- Prefer testing behaviour over implementation details — assert on rendered output and signal values, not internal method calls.
