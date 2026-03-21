using Projects;

namespace PaymentManager.Host.Local.Lib;

public static class AspireExtensions
{
    public const int FrontEndPort = 4200;
    public static PaymentManagerSqliteDatabase AddPaymentManagerSqliteDatabase(
        this IDistributedApplicationBuilder builder, string name)
        => new(builder.AddSqlite(name));

    public static PaymentManagerWebApi AddPaymentManagerWebApi(
        this IDistributedApplicationBuilder builder, string name, PaymentManagerSqliteDatabase sqliteDatabase)
        => new(builder.AddProject<PaymentManager_WebApi>(name)
                .WithReference(sqliteDatabase.Builder, "PaymentManager")
                .WithEnvironment("Cors__AllowedOrigins__0", $"http://localhost:{FrontEndPort.ToString()}"));

    public static PaymentManagerDatabaseSeeder AddPaymentManagerDatabaseSeeder(
        this IDistributedApplicationBuilder builder, string name, PaymentManagerSqliteDatabase sqliteDatabase)
        => new(builder.AddProject<PaymentManager_Host_Local_Database_Seeder>(name)
                .WithReference(sqliteDatabase.Builder, "PaymentManager"));

    public static PaymentManagerFrontend AddPaymentManagerFrontend(
        this IDistributedApplicationBuilder builder, string name, PaymentManagerWebApi webApi,
        string workingDirectory = "../../frontend")
        => new(builder.AddNpmApp(name, workingDirectory, "start:aspire")
                .WithReference(webApi.Builder)
                .WithHttpEndpoint(env: "PORT", targetPort: FrontEndPort, isProxied: false));

    public record PaymentManagerSqliteDatabase(IResourceBuilder<SqliteResource> Builder);
    public record PaymentManagerWebApi(IResourceBuilder<ProjectResource> Builder);
    public record PaymentManagerDatabaseSeeder(IResourceBuilder<ProjectResource> Builder);
    public record PaymentManagerFrontend(IResourceBuilder<NodeAppResource> Builder);
}
