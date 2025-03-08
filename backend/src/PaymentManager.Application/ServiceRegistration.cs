using Microsoft.Extensions.DependencyInjection;

namespace PaymentManager.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}