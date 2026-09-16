using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;

namespace PaymentManager.Infrastructure.Tests.Unit;

internal sealed class ReplaceContactsWithPeopleMigrationTests
{
    private const string PreMigrationId = "20260914212008_AddPaymentPayerGroup";

    [Test]
    public async Task Migrate_WithForeignKeysEnabled_ThrowsForeignKeyConstraintFailure()
    {
        var seeded = await SeedPreMigrationDatabaseAsync();
        try
        {
            await using var context = CreateContext(ConnectionString(seeded.DatabasePath, foreignKeys: true));

            var exception = await Should.ThrowAsync<Exception>(() => context.Database.MigrateAsync());

            var sqliteException = FindSqliteException(exception);
            sqliteException.ShouldNotBeNull();
            sqliteException!.SqliteErrorCode.ShouldBe(19);
        }
        finally
        {
            DeleteDatabase(seeded.DatabasePath);
        }
    }

    [Test]
    public async Task Migrate_WithForeignKeysDisabled_CopiesContactsAndAddsOwnerSplits()
    {
        var seeded = await SeedPreMigrationDatabaseAsync();
        try
        {
            await using (var context = CreateContext(ConnectionString(seeded.DatabasePath, foreignKeys: false)))
            {
                await context.Database.MigrateAsync();
            }

            await using (var context = CreateContext(ConnectionString(seeded.DatabasePath)))
            {
                var tables = await context.Database
                    .SqlQueryRaw<string>("SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table'")
                    .ToArrayAsync();
                tables.ShouldNotContain("Contacts");
                tables.ShouldContain("People");
                tables.ShouldContain("PayerGroupMembers");

                var people = await context.People.ToArrayAsync();
                people.Any(p => p.Id == seeded.ContactId && p.UserId == DefaultUserId && p.Name == "Jane Doe").ShouldBeTrue();
                people.Any(p => p.Id == DefaultUserId && p.Name == "Current User").ShouldBeTrue();

                (await PaymentSplitTotalAsync(context, seeded.SplitPaymentId)).ShouldBe(100m);
                (await PaymentSplitTotalAsync(context, seeded.UnsplitPaymentId)).ShouldBe(100m);

                var appliedMigrations = await context.Database
                    .SqlQueryRaw<string>("SELECT \"MigrationId\" AS \"Value\" FROM \"__EFMigrationsHistory\"")
                    .ToArrayAsync();
                appliedMigrations.ShouldContain("20260916194857_ReplaceContactsWithPeople");
                appliedMigrations.ShouldContain("20260916214138_RemovePersonIsSelf");
            }
        }
        finally
        {
            DeleteDatabase(seeded.DatabasePath);
        }
    }

    private static Guid DefaultUserId => new("11111111-1111-1111-1111-111111111111");

    private static string ConnectionString(string databasePath, bool? foreignKeys = null)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = databasePath };
        if (foreignKeys.HasValue)
        {
            builder.ForeignKeys = foreignKeys;
        }

        return builder.ToString();
    }

    private static PaymentManagerContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<PaymentManagerContext>().UseSqlite(connectionString).Options);

    private static async Task<decimal> PaymentSplitTotalAsync(PaymentManagerContext context, Guid paymentId)
    {
        var splits = await context.PaymentSplits
            .Where(s => s.PaymentId == paymentId)
            .ToArrayAsync();
        return splits.Sum(s => s.Percentage);
    }

    private static SqliteException? FindSqliteException(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is SqliteException sqliteException)
            {
                return sqliteException;
            }

            exception = exception.InnerException;
        }

        return null;
    }

    private static async Task<SeededDatabase> SeedPreMigrationDatabaseAsync()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "payment-manager-migration-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(databaseDirectory);
        var databasePath = Path.Combine(databaseDirectory, "paymentmanager.db");

        var paymentSourceId = Guid.NewGuid();
        var payeeId = Guid.NewGuid();
        var splitPaymentId = Guid.NewGuid();
        var unsplitPaymentId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        await using (var context = CreateContext(ConnectionString(databasePath, foreignKeys: true)))
        {
            await context.Database.GetService<IMigrator>().MigrateAsync(PreMigrationId);

            context.PaymentSources.Add(new PaymentSource { Id = paymentSourceId, UserId = DefaultUserId, Name = "Current" });
            context.Payees.Add(new Payee { Id = payeeId, UserId = DefaultUserId, Name = "Landlord" });
            context.Payments.Add(CreatePayment(splitPaymentId, paymentSourceId, payeeId));
            context.Payments.Add(CreatePayment(unsplitPaymentId, paymentSourceId, payeeId));
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Contacts\" (\"Id\", \"UserId\", \"Name\") VALUES ({0}, {1}, {2})",
                contactId, DefaultUserId, "Jane Doe");
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"PaymentSplits\" (\"PaymentId\", \"ContactId\", \"Percentage\") VALUES ({0}, {1}, {2})",
                splitPaymentId, contactId, 40m);
        }

        SqliteConnection.ClearAllPools();
        return new SeededDatabase(databasePath, contactId, splitPaymentId, unsplitPaymentId);
    }

    private static Payment CreatePayment(Guid id, Guid paymentSourceId, Guid payeeId) => new()
    {
        Id = id,
        UserId = DefaultUserId,
        PaymentSourceId = paymentSourceId,
        PayeeId = payeeId,
        InitialAmount = 100m,
        Currency = "GBP",
        Frequency = PaymentFrequency.Monthly,
        Direction = PaymentDirection.Outgoing,
        StartDate = new DateOnly(2026, 1, 1)
    };

    private static void DeleteDatabase(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        var directory = Path.GetDirectoryName(databasePath)!;
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed record SeededDatabase(string DatabasePath, Guid ContactId, Guid SplitPaymentId, Guid UnsplitPaymentId);
}
