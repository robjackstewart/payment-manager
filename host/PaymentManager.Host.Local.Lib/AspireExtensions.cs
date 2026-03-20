using Projects;

namespace PaymentManager.Host.Local.Lib;

public static class AspireExtensions
{
    public static PaymentManagerSqliteDatabase AddPaymentManagerSqliteDatabase(
        this IDistributedApplicationBuilder builder, string name)
        => new(builder.AddSqlite(name));

    public static PaymentManagerWebApi AddPaymentManagerWebApi(
        this IDistributedApplicationBuilder builder, string name, PaymentManagerSqliteDatabase sqliteDatabase)
        => new(builder.AddProject<PaymentManager_WebApi>(name)
                .WithReference(sqliteDatabase.Builder, "PaymentManager"));

    public static PaymentManagerDatabaseSeeder AddPaymentManagerDatabaseSeeder(
        this IDistributedApplicationBuilder builder, string name, PaymentManagerSqliteDatabase sqliteDatabase)
        => new(builder.AddProject<PaymentManager_Host_Local_Database_Seeder>(name)
                .WithReference(sqliteDatabase.Builder, "PaymentManager"));

    public record PaymentManagerSqliteDatabase(IResourceBuilder<SqliteResource> Builder);
    public record PaymentManagerWebApi(IResourceBuilder<ProjectResource> Builder);
    public record PaymentManagerDatabaseSeeder(IResourceBuilder<ProjectResource> Builder);
}
