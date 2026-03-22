---
applyTo: backend/**
---

# Backend Coding Instructions

## Architecture

The backend follows Clean Architecture. Each concern maps to exactly one project:

| Project | Responsibility |
|---|---|
| `PaymentManager.WebApi` | Presentation layer — HTTP endpoints, request/response models, middleware |
| `PaymentManager.Application` | Core application layer — commands, queries, handlers (MediatR), validators |
| `PaymentManager.Domain` | Domain layer — entities, value objects, domain events, domain exceptions |
| `PaymentManager.Infrastructure.<Dependency>` | One project per external infrastructure dependency (e.g. `PaymentManager.Infrastructure.Sqlite` for SQLite/EF Core) |
| `PaymentManager.Common` | Cross-cutting concerns shared across layers — extensions, helpers, abstractions |

### Rules
- Never reference a higher layer from a lower layer (e.g. Domain must not reference Application or Infrastructure).
- Application depends only on Domain and Common.
- Infrastructure projects depend on Application and Domain.
- WebApi depends on Application, Infrastructure, and Common.
- Add a new `PaymentManager.Infrastructure.<Name>` project for each new external infrastructure dependency rather than adding it to an existing infrastructure project.

## Project Structure

```
backend/
  src/
    PaymentManager.WebApi/
    PaymentManager.Application/
    PaymentManager.Domain/
    PaymentManager.Infrastructure.Sqlite/          # SQLite / EF Core
    PaymentManager.Common/
  tests/
    unit/
      PaymentManager.WebApi.Tests.Unit/
      PaymentManager.Application.Tests.Unit/
      PaymentManager.Domain.Tests.Unit/
      PaymentManager.Infrastructure.Sqlite.Tests.Unit/
      PaymentManager.Common.Tests.Unit/
    integration/
      PaymentManager.WebApi.Tests.Integration/
    PaymentManager.Tests.Common/            # Shared test helpers
```

- Every source project has a corresponding unit test project under `tests/unit/`.
- Every presentation-layer project (e.g. `PaymentManager.WebApi`) also has an integration test project under `tests/integration/`.

## Unit Tests

**Framework:** NUnit + FakeItEasy + Shouldly

### Parallel execution rule
Unit test classes **must not** have instance properties or instance fields. All data, fakes, and system-under-test instances must be created inline inside each test method or in `static` helpers/factory methods. This ensures NUnit can run all test classes fully in parallel without shared-state conflicts.

```csharp
// ✅ Correct — no instance state
public class CreatePaymentHandlerTests
{
    [Test]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var repository = A.Fake<IPaymentRepository>();
        var handler = new CreatePaymentHandler(repository);
        // ...
    }
}

// ❌ Wrong — instance fields break parallel safety
public class CreatePaymentHandlerTests
{
    private IPaymentRepository _repository;   // do not do this
    private CreatePaymentHandler _handler;    // do not do this

    [SetUp]
    public void SetUp() { ... }
}
```

- Do not use `[SetUp]` / `[TearDown]` methods on instance members.
- Static helper or factory methods are permitted.

## Integration Tests

**Framework:** NUnit + `Microsoft.AspNetCore.Mvc.Testing` + .NET Aspire Testing

Integration tests for presentation-layer projects use an `ApplicationUnderTest` class that manages the full application lifecycle.

### ApplicationUnderTest pattern
- `ApplicationUnderTest` encapsulates `WebApplicationFactory<TEntryPoint>` and any Aspire resource setup.
- It is created once per test class and disposed after the class finishes — use `[OneTimeSetUp]` / `[OneTimeTearDown]` **as static members** to stay consistent with the no-instance-state rule.
- Tests call into `ApplicationUnderTest` to get `HttpClient` instances or resolve services; they do not configure the host themselves.

```csharp
// ✅ Correct integration test structure
public class PaymentsEndpointTests
{
    private static ApplicationUnderTest _app;

    [OneTimeSetUp]
    public static async Task StartApp() =>
        _app = await ApplicationUnderTest.StartAsync();

    [OneTimeTearDown]
    public static async Task StopApp() =>
        await _app.DisposeAsync();

    [Test]
    public async Task GetPayments_ReturnsOk()
    {
        using var client = _app.CreateClient();
        var response = await client.GetAsync("/payments");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

## Naming Conventions

- Commands: `<Action><Entity>Command` (e.g. `CreatePaymentCommand`)
- Queries: `<Action><Entity>Query` (e.g. `GetPaymentsQuery`)
- Handlers: `<Command|Query>Handler` (e.g. `CreatePaymentCommandHandler`)
- Validators: `<Command|Query>Validator`
- Repositories (interfaces): `I<Entity>Repository`
- EF Core DbContext: `PaymentManagerContext` (write), `ReadOnlyPaymentManagerContext` (read)

## Package Management

All NuGet package versions are managed centrally in `Directory.Packages.props` at the repository root. Do not specify versions in individual `.csproj` files — add new packages to `Directory.Packages.props` first, then reference them without a version in the project file.

## Collection Materialisation

Use `ToArray` / `ToArrayAsync` when materialising a LINQ query or EF Core query whose result will only be **read**. Use `ToList` / `ToListAsync` only when the materialised collection will be **mutated** afterwards (e.g. items added or removed via `Add`, `Remove`, `RemoveRange`).

```csharp
// ✅ Correct — result is only iterated / projected
var payments = await context.Payments.Where(...).ToArrayAsync(ct);

// ✅ Correct — result is mutated (RemoveRange requires a mutable list)
var splits = await context.PaymentSplits.Where(...).ToListAsync(ct);
context.PaymentSplits.RemoveRange(splits);

// ❌ Wrong — ToList used but the collection is never mutated
var payments = await context.Payments.Where(...).ToListAsync(ct);
```

## Collection Expressions

Use C# 12 collection expressions (`[...]`) in preference to any older construction syntax. The compiler infers the target type from context, so the same syntax works for arrays, `List<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, and other collection interfaces.

| Instead of | Use |
|---|---|
| `Array.Empty<T>()` / `Enumerable.Empty<T>()` | `[]` |
| `new T[] { x, y }` / `new[] { x, y }` | `[x, y]` |
| `new List<T> { x, y }` | `[x, y]` |
| `a.Concat(b).ToArray()` | `[.. a, .. b]` |

```csharp
// ❌ Old-style
PaymentSplit[] empty = Array.Empty<PaymentSplit>();
var ids = new[] { id1, id2 };
List<string> names = new List<string> { "Alice", "Bob" };
var all = existing.Concat(newItems).ToArray();

// ✅ Collection expression
PaymentSplit[] empty = [];
var ids = (Guid[])[id1, id2];
List<string> names = ["Alice", "Bob"];
var all = [.. existing, .. newItems];
```

## Property Grouping

When two or more properties on a class share a common prefix, extract them into a nested `record` named after the prefix. This reduces noise in constructor signatures and call sites, and makes the relationship between the values explicit.

```csharp
// ❌ Wrong — flat properties with a shared prefix
public record PaymentDto(decimal UserSharePercentage, decimal UserShareValue, ...);

// ✅ Correct — grouped into a nested record
public record PaymentDto(ShareInfo UserShare, ...)
{
    public record ShareInfo(decimal Percentage, decimal Value);
}

// Usage
var dto = new PaymentDto(new ShareInfo(50m, 100m), ...);
var pct = dto.UserShare.Percentage;
```

Apply this rule at any layer — DTOs, response records, domain value objects, and view models.

