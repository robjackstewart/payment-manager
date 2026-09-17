using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Host.Local.Common;
using PaymentManager.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration.Get<PaymentManager.Host.Local.Database.Seeder.Configuration>();
Guard.IsNotNull(configuration);
// Foreign keys are disabled so SQLite table-rebuild migrations work; PRAGMA foreign_keys
// is a no-op inside EF's migration transaction, so it is applied via the connection string.
builder.Services.AddPaymentManagerInfrastructure(new Configuration
{
    DatabaseConnectionString = $"{configuration.ConnectionStrings.PaymentManager};Foreign Keys=False"
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

foreach (var payerGroup in Seed.PayerGroups)
{
    if (!await context.PayerGroups.AnyAsync(h => h.Id == payerGroup.Id))
    {
        context.PayerGroups.Add(payerGroup);
        logger.LogInformation("Seeded payer group: {Name}", payerGroup.Name);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var person in Seed.People)
{
    if (!await context.People.AnyAsync(p => p.Id == person.Id))
    {
        context.People.Add(person);
        logger.LogInformation("Seeded person: {Name}", person.Name);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var member in Seed.PayerGroupMembers)
{
    if (!await context.PayerGroupMembers.AnyAsync(m => m.PayerGroupId == member.PayerGroupId && m.PersonId == member.PersonId))
    {
        context.PayerGroupMembers.Add(member);
        logger.LogInformation("Seeded payer group member: ({PayerGroupId}, {PersonId})", member.PayerGroupId, member.PersonId);
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
        logger.LogInformation("Seeded payment: {Id}", payment.Id);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var split in Seed.PaymentSplits)
{
    if (!await context.PaymentSplits.AnyAsync(s => s.PaymentId == split.PaymentId && s.PersonId == split.PersonId))
    {
        context.PaymentSplits.Add(split);
        logger.LogInformation("Seeded payment split: ({PaymentId}, {PersonId})", split.PaymentId, split.PersonId);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var effectiveSplit in Seed.EffectivePaymentSplits)
{
    if (!await context.EffectivePaymentSplits.AnyAsync(s => s.PaymentId == effectiveSplit.PaymentId && s.EffectiveDate == effectiveSplit.EffectiveDate && s.PersonId == effectiveSplit.PersonId))
    {
        context.EffectivePaymentSplits.Add(effectiveSplit);
        logger.LogInformation("Seeded effective payment split: ({PaymentId}, {EffectiveDate}, {PersonId}) = {Percentage}%", effectiveSplit.PaymentId, effectiveSplit.EffectiveDate, effectiveSplit.PersonId, effectiveSplit.Percentage);
    }
}
await context.SaveChanges(CancellationToken.None);

foreach (var effectiveValue in Seed.EffectivePaymentValues)
{
    if (!await context.EffectivePaymentValues.AnyAsync(v => v.PaymentId == effectiveValue.PaymentId && v.EffectiveDate == effectiveValue.EffectiveDate))
    {
        context.EffectivePaymentValues.Add(effectiveValue);
        logger.LogInformation("Seeded effective payment value: ({PaymentId}, {EffectiveDate}) = {Amount}", effectiveValue.PaymentId, effectiveValue.EffectiveDate, effectiveValue.Amount);
    }
}
await context.SaveChanges(CancellationToken.None);

logger.LogInformation("Seeding complete");
