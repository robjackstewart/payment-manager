using PaymentManager.Application;
using PaymentManager.Infrastructure;

namespace PaymentManager.WebApi;

public static class ServiceRegistration
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        return services.AddApplication()
            .AddInfrastructure();
    }
}