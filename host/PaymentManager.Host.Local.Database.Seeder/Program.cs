using CommunityToolkit.Diagnostics;
using PaymentManager.Host.Local.Database.Seeder;
using PaymentManager.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration.Get<PaymentManager.Host.Local.Database.Seeder.Configuration>();
Guard.IsNotNull(configuration);
builder.Services.AddPaymentManagerInfrastructure(new PaymentManager.Infrastructure.Configuration
{
    DatabaseConnectionString = configuration.DatabaseConnectionString
});

var host = builder.Build();
host.Run();
