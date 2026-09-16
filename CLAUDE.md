# payment-manager

A payment-splitting app with three parts:

- **`backend/`** — .NET 10 Web API, Clean Architecture. See `backend/CLAUDE.md`.
- **`frontend/`** — Angular 21 + Material, signals-first, zoneless. See `frontend/CLAUDE.md`.
- **`host/`** — .NET Aspire local dev host that wires up and runs the full stack. See `host/CLAUDE.md`.

Each area's `CLAUDE.md` loads automatically when working in that directory — read
it for the detailed rules before making changes there.

## Commands

Run via [Task](https://taskfile.dev) from the repo root:

```bash
task install-dependencies    # restore/npm install for both backend and frontend
task build                   # build backend + frontend
task lint                    # dotnet format --verify-no-changes + npm run lint
task lint:fix                # dotnet format + npm run lint -- --fix
task test                    # backend tests only (dotnet test) — see note below
task run                     # backend:api:run + frontend:run
```

`task run` starts the API and frontend directly. To run the full Aspire-orchestrated
stack (API, database, seeders) with one command, use `task host:run` (or
`cd host/PaymentManager.Host.Local && dotnet run`) instead — see `host/CLAUDE.md`.

**Note:** root `task test` only runs backend tests. Frontend Vitest specs run via
`task frontend:test` (`npm run test`).

Backend-only and frontend-only variants of most tasks exist as `task backend:<name>`
and `task frontend:<name>` — see `backend/Taskfile.yml` and `frontend/Taskfile.yml`
for the full list (migrations, publish, production build, etc.).

## Domain vocabulary

- **User** — the signed-in owner of the data. Currently single-user (a hardcoded default
  user); every entity below is scoped to a `UserId`. The owner is *not* modelled as a
  `Person` and is never special-cased.
- **Person** — anyone money can be apportioned to, including the owner (who is just another
  person). People are generic — there is no "self" flag and no implicit owner person. Each
  person may belong to any number of **PayerGroups** via `PayerGroupMember`.
- **PayerGroup** — an *optional* named set of people (e.g. "Family", "Housemates"). There is
  no implicit/"Personal" group: payments that are not in a group are simply split across
  individual people. A group defines *who a grouped payment may be split with*, not
  percentages — percentages live per payment.
- **Payee** — who a payment is with: who is paid (bills) or who pays the user (income).
- **PaymentSource** — the account or card a payment moves through, in either direction.
- **Payment** — a bill or income entry: an amount, a `Frequency` (Once/Monthly/Annually),
  a `Direction` (Outgoing/Incoming), and optionally a `PayerGroupId`.
- **PaymentSplit** — a `(PaymentId, PersonId, Percentage)` row apportioning one payment.
  Every payment has at least one split and its splits total exactly 100%; a grouped
  outgoing payment may only be split across that group's members, while an ungrouped
  payment may be split across any of the user's people, and income never carries a group —
  all enforced server-side in `PaymentSplitGuard`
  (`backend/src/PaymentManager.Application/Common/PaymentSplitGuard.cs`). The owner's share
  is stored like anyone else's; `SplitPaymentCalculator` only allocates the split values.
- **EffectivePaymentValue** — a date-stepped override of a payment's amount from a given
  date onward (`Payment.InitialAmount` is the value before the first override).
- **Occurrence** — one computed, non-persisted instance of a payment landing on a specific
  date, expanded from its frequency for a date range (`GetPaymentOccurrences`).

## Cross-cutting conventions

- **NuGet package versions** are centralized in `Directory.Packages.props` at the
  repo root — never pin a version in an individual `.csproj`. Details in
  `backend/CLAUDE.md`.
- **Tests must not rely on shared instance/hook state.** Both the backend (NUnit —
  no instance fields, no `[SetUp]`/`[TearDown]`) and frontend (Vitest — no
  `beforeEach`/`afterEach`, explicit `setup()` instead) enforce this so tests stay
  parallel-safe and isolated. See the area files for the exact pattern in each
  framework.
