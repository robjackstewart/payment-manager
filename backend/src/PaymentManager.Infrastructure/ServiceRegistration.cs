using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentManager.Application.Common;

namespace PaymentManager.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseConnectionString = configuration.GetConnectionString("PaymentManager");

        Guard.IsNotNullOrWhiteSpace(databaseConnectionString);

        return services.AddDbContext<PaymentManagerContext>(options =>
            options.UseSqlite(databaseConnectionString))
            .AddScoped<IPaymentManagerContext, PaymentManagerContext>()
            .AddScoped<IReadOnlyPaymentManagerContext, ReadOnlyPaymentManagerContext>();
    }
}