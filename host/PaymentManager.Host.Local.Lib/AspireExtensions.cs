using System;
using Projects;

namespace PaymentManager.Host.Local.Lib;

public static class AspireExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public PaymentManagerSqliteDatabase AddPaymentManagerSqliteDatabase(string name)
            => new(builder.AddSqlite(name));
        public PaymentManagerWebApi AddPaymentManagerWebApi(string name, PaymentManagerSqliteDatabase sqliteDatabase)
            => new(builder.AddProject<PaymentManager_WebApi>(name)
                    .WithReference(sqliteDatabase.Builder, "PaymentManager"));
    }

    public record PaymentManagerSqliteDatabase(IResourceBuilder<SqliteResource> Builder);

    public record PaymentManagerWebApi(IResourceBuilder<ProjectResource> Builder);

    public record PaymentManager(PaymentManagerWebApi WebApi, PaymentManagerSqliteDatabase SqliteDatabase);
}
