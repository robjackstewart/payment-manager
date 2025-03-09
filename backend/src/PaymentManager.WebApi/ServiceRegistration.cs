using Microsoft.OpenApi.Models;
using PaymentManager.Application;
using PaymentManager.Infrastructure;

namespace PaymentManager.WebApi;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddPaymentManagerApplication()
            .AddPaymentManagerInfrastructure(configuration)
            .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1",
                        new OpenApiInfo { Title = "Payment Manager", Version = "v1" });
                    c.CustomSchemaIds(type => type.FullName);
                }).AddOpenApi();

        return services;
    }
}