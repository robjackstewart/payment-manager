using PaymentManager.Host.Local.Lib;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddPaymentManagerSqliteDatabase("database");

await builder.Build().RunAsync();

