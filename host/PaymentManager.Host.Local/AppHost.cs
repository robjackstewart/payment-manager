using PaymentManager.Host.Local.Lib;

var builder = DistributedApplication.CreateBuilder(args);

var sqlite = builder.AddPaymentManagerSqliteDatabase("sqlite");

var seeder = builder.AddPaymentManagerDatabaseSeeder("database-seeder", sqlite);

var api = builder.AddPaymentManagerWebApi("web-api", sqlite);

await builder.Build().RunAsync();
