using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using PaymentManager.Application;
using PaymentManager.Infrastructure;
using PaymentManager.WebApi.Endpoints;

namespace PaymentManager.WebApi;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        var uiUrl = configuration["UI:Url"];
        Guard.IsNotNullOrWhiteSpace(uiUrl);

        services
            .AddPaymentManagerApplication()
            .AddPaymentManagerInfrastructure(configuration)
            .AddOpenApi(opt =>
            {
                opt.CreateSchemaReferenceId = (jsonTypeInfo) =>
                {
                    // Normal behavior if not a nested class
                    if (!jsonTypeInfo.Type.IsNested)
                    {
                        return OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
                    }

                    // Concatenate nested class name with parent class name
                    return $"{jsonTypeInfo.Type.DeclaringType!.Name}{jsonTypeInfo.Type.Name}";
                };
            })
            .AddProblemDetails()
            .AddExceptionHandler<ExceptionHandler>()
            .AddCors(opt =>
            {
                opt.AddPolicy(Constants.Cors.ALLOW_UI_POLICY_NAME, builder =>
                {
                    builder
                        .WithOrigins(uiUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
        => app.MapGetUserEndpoint()
            .MapCreateUserEndpoint();
}