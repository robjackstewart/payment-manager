---
applyTo: frontend/**
---

# Frontend Coding Instructions

## Framework

Angular 21.x with Angular Material. TypeScript strict mode is enabled.

## Standalone Components

All components are `standalone: true`. **Import specific standalone components and directives rather than whole `*Module` barrel imports.** This makes each component's dependencies explicit and keeps the dependency graph clear.

```typescript
// ✅ Correct — import only the specific components used in the template
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef,
         MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef,
         MatRow, MatRowDef } from '@angular/material/table';

@Component({
  standalone: true,
  imports: [MatButton, MatIconButton, MatTable, MatColumnDef, ...],
})

// ❌ Wrong — module imports pull in symbols you may not use
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
```

- Match imports exactly to what the template uses — no more, no less.
- `ReactiveFormsModule` is the accepted import for reactive forms (its directives are tree-shaken correctly).
- Never add `NgModule`-based wrappers for new features — the app has no feature modules.

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

## View Models and Template Purity

**Templates must not contain logic.** All calculations, formatting, conditional expressions, and pipe transforms belong in the component class, not in the template.

This means:
- No ternary expressions (`x ? a : b`) in templates
- No logical OR / nullish coalescing fallbacks (`x || '—'`, `x ?? y`)
- No pipe transforms with dynamic arguments (`| currency: item.currency`, `| date: 'mediumDate'`)
- No method calls that compute values per row (`getLabel(item.freq)`, `calcShare(item)`)

Instead, derive a **view model** using a `computed()` signal that maps raw API data into display-ready objects once, whenever the source data changes.

```typescript
// ✅ Correct — compute display values once in a view model signal
interface PaymentViewModel {
  payeeName: string;
  formattedAmount: string;   // e.g. "$120.00"
  yourShareDisplay: string;  // e.g. "75%"
  formattedDate: string;
  _raw: Payment;             // keep original for actions (edit/delete)
}

@Component({
  providers: [CurrencyPipe, DatePipe],  // inject pipes for formatting
})
export class PaymentListComponent {
  private readonly currencyPipe = inject(CurrencyPipe);
  private readonly datePipe = inject(DatePipe);

  readonly payments = signal<Payment[]>([]);

  readonly paymentsViewModel = computed<PaymentViewModel[]>(() =>
    this.payments().map(p => ({
      payeeName: this.payeesMap()[p.payeeId] ?? p.payeeId,
      formattedAmount: this.currencyPipe.transform(p.amount, p.currency) ?? String(p.amount),
      yourShareDisplay: `${Math.max(0, 100 - p.splits.reduce((s, x) => s + x.percentage, 0))}%`,
      formattedDate: this.datePipe.transform(p.startDate, 'mediumDate') ?? p.startDate,
      _raw: p,
    }))
  );
}
```

```html
<!-- ✅ Template reads pre-computed values — no logic -->
<td mat-cell *matCellDef="let vm">{{ vm.payeeName }}</td>
<td mat-cell *matCellDef="let vm">{{ vm.formattedAmount }}</td>
<button (click)="edit(vm._raw)">Edit</button>

<!-- ❌ Wrong — logic in template -->
<td mat-cell *matCellDef="let p">{{ payeesMap()[p.payeeId] ?? p.payeeId }}</td>
<td mat-cell *matCellDef="let p">{{ p.amount | currency: p.currency }}</td>
```

For values derived from static injected data (e.g. dialog title from `MAT_DIALOG_DATA`), a plain `readonly` class property is sufficient — `computed()` is not needed when the source never changes:

```typescript
// ✅ Static derived value — plain readonly property
readonly title = this.data.payment ? 'Edit Payment' : 'New Payment';
readonly submitLabel = this.data.payment ? 'Save' : 'Create';
```

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

## Bundle Size

The initial bundle must stay under **400 kB** (warning) / **600 kB** (error) as enforced by `angular.json` budgets. These limits are verified on every build, including CI.

### Rules

**Route-level lazy loading is mandatory.** Every page must be registered with `loadComponent` + `import()` in `app.routes.ts`. Never import a page component at the top of a module or config file.

**Dialog components must be lazy-loaded.** Dialogs are not visible on page load — load them on first user interaction using `import()` inside the open method:

```typescript
// ✅ Correct — dialog chunk loaded on first open
async openCreateDialog(): Promise<void> {
  const { PaymentFormDialogComponent } = await import('./payment-form-dialog/payment-form-dialog');
  this.dialog.open(PaymentFormDialogComponent, { width: '520px', data: { ... } });
}

// ❌ Wrong — dialog bundled with the route chunk whether used or not
import { PaymentFormDialogComponent } from './payment-form-dialog/payment-form-dialog';
openCreateDialog() { this.dialog.open(PaymentFormDialogComponent, ...); }
```

**No barrel `index.ts` re-exports.** Barrels force the bundler to evaluate entire directories, defeating tree-shaking. Always import by direct file path.

**Scope heavy providers to the components that need them.** Providers added to `app.config.ts` are included in the initial bundle for every user. Add them to the `providers` array of the standalone component instead:

```typescript
// ✅ Correct — date adapter loaded only with the dashboard chunk
@Component({
  providers: [provideNativeDateAdapter()],
})
export class DashboardComponent { }

// ❌ Wrong — loaded for all users, even those who never visit the dashboard
// app.config.ts
providers: [provideNativeDateAdapter()]
```

**Use `provideZonelessChangeDetection()`** (already configured). Never add `zone.js` back.

### Diagnosing regressions

When the budget warning fires, run:

```bash
npm run analyze
```

This builds with source maps and opens `source-map-explorer` to show exactly which files and packages are contributing to each chunk.



**Framework:** Vitest + JSDOM

- Test files are colocated with source files (`*.spec.ts`).
- Use `TestBed` for component/service tests that need the Angular DI container.
- Prefer testing behaviour over implementation details — assert on rendered output and signal values, not internal method calls.

## Test Structure

**No `beforeEach` or `afterEach` in spec files.** All setup logic belongs in an explicit `setup()` function that every test must call. A global `afterEach` in `src/test-setup.ts` (registered via `angular.json` `setupFiles`) handles `vi.restoreAllMocks()` + `vi.clearAllMocks()` after every test — do not add this to individual spec files.

This pattern enforces that each test is self-contained and fails loudly if `setup()` is forgotten, since mocks are wiped clean after every test.

```typescript
// ✅ Correct — explicit setup(), no hooks in spec file
function setup() {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [MyService, { provide: ApiClient, useValue: { getData: vi.fn() } }],
  });
  const service = TestBed.inject(MyService);
  const api = TestBed.inject(ApiClient) as { getData: ReturnType<typeof vi.fn> };
  return { service, api };
}

describe('MyService', () => {
  it('returns data from the API', async () => {
    const { service, api } = setup();
    api.getData.mockReturnValue(of([{ id: 1 }]));

    const result = await firstValueFrom(service.getAll());

    expect(result).toEqual([{ id: 1 }]);
  });
});

// ❌ Wrong — shared state via beforeEach breaks test isolation
describe('MyService', () => {
  let service: MyService;
  let api: { getData: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { getData: vi.fn() };
    TestBed.configureTestingModule({ ... });
    service = TestBed.inject(MyService);
  });
});
```

### setup() responsibilities

`setup()` must handle, in this order:
1. **State cleanup** — `localStorage.clear()`, `document.body.classList.remove(...)`, etc.
2. **Spy setup** — `vi.spyOn(...)` for any external package methods
3. **TestBed** — `TestBed.resetTestingModule()` then `TestBed.configureTestingModule(...)`
4. **Component/service creation** — `TestBed.createComponent(...)` or `TestBed.inject(...)`
5. **Return** — all fixtures, mocks, and instances the test will need

## Mocking External Packages

**Never use `vi.mock('package-name', ...)` for node_modules packages.** Angular's `@angular/build:unit-test` builder sets `externalPackages: true` in esbuild, which leaves all node_modules unbundled. When vitest processes `vi.mock(...)` for an external package, it tries to resolve the package to a file path — but since esbuild never bundled it, the resolution returns `undefined` and vitest crashes with `TypeError: Cannot read properties of undefined (reading 'trim')`. This failure is flaky when multiple spec files mock the same package.

**Use `vi.spyOn` instead.** It operates at runtime on already-imported object properties and never needs to resolve a module path.

```typescript
// ✅ Correct — spy on the imported object in setup()
import { AgCharts } from 'ag-charts-community';

function setup() {
  const fakeChart = {
    update: vi.fn().mockResolvedValue(undefined),
    destroy: vi.fn(),
  };
  vi.spyOn(AgCharts, 'create').mockReturnValue(
    fakeChart as unknown as ReturnType<typeof AgCharts.create>,
  );
  // ... TestBed setup ...
  return { fakeChart };
}

// ❌ Wrong — crashes with Angular's esbuild test runner
vi.mock('ag-charts-community', () => ({
  AgCharts: { create: vi.fn() },
}));
```

Use `vi.mocked(fn)` for type-safe access to mock call history:

```typescript
const options = vi.mocked(AgCharts.create).mock.calls[0][0] as any;
expect(options.theme.baseTheme).toBe('ag-material');
```
