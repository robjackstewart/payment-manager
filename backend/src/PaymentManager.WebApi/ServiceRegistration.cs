using Microsoft.OpenApi.Models;
using PaymentManager.Application;
using PaymentManager.Infrastructure;
using PaymentManager.WebApi.Endpoints;

namespace PaymentManager.WebApi;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddPaymentManagerApplication()
            .AddPaymentManagerInfrastructure(configuration)
            .AddOpenApi()
            .AddProblemDetails()
            .AddExceptionHandler<ExceptionHandler>();

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
        => app.MapGetUserEndpoint()
            .MapCreateUserEndpoint();
}