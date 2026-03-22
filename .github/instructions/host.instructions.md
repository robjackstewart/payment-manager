---
applyTo: host/**
---

# Host Coding Instructions

## Purpose

The `host/` directory contains the .NET Aspire-based local development host. Its job is to wire up and run all application resources (API, database, seeders, etc.) so that a developer can run the full stack locally with a single command.

## Project Structure

```
host/
  PaymentManager.Host.Local/               # AppHost — entry point, always runnable
  PaymentManager.Host.Local.Lib/           # All Aspire orchestration logic (class library)
  PaymentManager.Host.Local.Infrastructure/# Infrastructure concerns (e.g. database seeding integration)
  PaymentManager.Host.Local.Common/        # Shared utilities across host projects
  PaymentManager.Host.Local.Database.Seeder/ # Database seeding executable
```

## Rules

### Host.Local must always be runnable
`PaymentManager.Host.Local` is the Aspire AppHost entry point. It must **always** compile and run successfully without manual pre-steps. Do not add logic directly into `AppHost.cs` that could break the startup path. Keep `AppHost.cs` thin — it should only wire up resources defined elsewhere.

### Aspire logic belongs in Host.Local.Lib
All Aspire orchestration code (resource definitions, configuration builders, extension methods for `IDistributedApplicationBuilder`) must live in `PaymentManager.Host.Local.Lib`. `Host.Local` references this library and delegates to it.

This keeps Aspire logic testable and reusable without depending on the AppHost executable itself.

```csharp
// ✅ Correct — AppHost delegates to the library
// PaymentManager.Host.Local/AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);
builder.AddPaymentManagerResources();   // extension method from Host.Local.Lib
await builder.Build().RunAsync();

// ✅ Correct — orchestration detail lives in Host.Local.Lib
// PaymentManager.Host.Local.Lib/DistributedApplicationBuilderExtensions.cs
public static IDistributedApplicationBuilder AddPaymentManagerResources(
    this IDistributedApplicationBuilder builder)
{
    // resource wiring here
    return builder;
}
```

### Infrastructure concerns belong in Host.Local.Infrastructure
Any infrastructure-level host concerns — such as integrating with the database seeder, configuring connection strings, or wrapping external resource clients — go in `PaymentManager.Host.Local.Infrastructure`.

### Shared utilities belong in Host.Local.Common
Helpers, constants, or extension methods used by more than one host project go in `PaymentManager.Host.Local.Common`.

## Running Locally

Start the full local stack with:

```bash
task run
# or directly:
cd host/PaymentManager.Host.Local && dotnet run
```

The Aspire dashboard will show all running resources and their logs.
