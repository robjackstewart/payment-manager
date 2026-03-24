using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;

namespace PaymentManager.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerInfrastructure(this IServiceCollection services, Configuration configuration)
    {
        var databaseConnectionString = configuration.DatabaseConnectionString;

        Guard.IsNotNullOrWhiteSpace(databaseConnectionString);

        return services.AddDbContext<PaymentManagerContext>(options =>
            options.UseSqlite(databaseConnectionString))
            .AddScoped<IPaymentManagerContext, PaymentManagerContext>()
            .AddScoped<IReadOnlyPaymentManagerContext, ReadOnlyPaymentManagerContext>();
    }
}