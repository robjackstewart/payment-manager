using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PaymentManager.Application.Common;
using PaymentManager.Host.Local.Common;
using PaymentManager.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration.Get<PaymentManager.Host.Local.Database.Seeder.Configuration>();
Guard.IsNotNull(configuration);
builder.Services.AddPaymentManagerInfrastructure(new Configuration
{
    DatabaseConnectionString = configuration.ConnectionStrings.PaymentManager
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<IPaymentManagerContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Applying migrations...");
await context.Database.MigrateAsync();
logger.LogInformation("Migrations applied");

logger.LogInformation("Seeding data...");

foreach (var user in Seed.Users)
{
    if (!await context.Users.AnyAsync(u => u.Id == user.Id))
    {
        context.Users.Add(user);
        logger.LogInformation("Seeded user: {Name}", user.Name);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var paymentSource in Seed.PaymentSources)
{
    if (!await context.PaymentSources.AnyAsync(ps => ps.Id == paymentSource.Id))
    {
        context.PaymentSources.Add(paymentSource);
        logger.LogInformation("Seeded payment source: {Name}", paymentSource.Name);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var payee in Seed.Payees)
{
    if (!await context.Payees.AnyAsync(p => p.Id == payee.Id))
    {
        context.Payees.Add(payee);
        logger.LogInformation("Seeded payee: {Name}", payee.Name);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var payment in Seed.Payments)
{
    if (!await context.Payments.AnyAsync(p => p.Id == payment.Id))
    {
        context.Payments.Add(payment);
        logger.LogInformation("Seeded payment: {Id} ({Amount})", payment.Id, payment.Amount);
    }
}
await context.SaveChanges(CancellationToken.None);

logger.LogInformation("Seeding complete");
