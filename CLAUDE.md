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

## Cross-cutting conventions

- **NuGet package versions** are centralized in `Directory.Packages.props` at the
  repo root — never pin a version in an individual `.csproj`. Details in
  `backend/CLAUDE.md`.
- **Tests must not rely on shared instance/hook state.** Both the backend (NUnit —
  no instance fields, no `[SetUp]`/`[TearDown]`) and frontend (Vitest — no
  `beforeEach`/`afterEach`, explicit `setup()` instead) enforce this so tests stay
  parallel-safe and isolated. See the area files for the exact pattern in each
  framework.
