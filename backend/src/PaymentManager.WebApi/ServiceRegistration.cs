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
                    // Concatenate all nested type parents' names together
                    static string GetFullNestedTypeName(Type type)
                    {
                        if (type.DeclaringType == null)
                        {
                            return type.Name;
                        }
                        return $"{GetFullNestedTypeName(type.DeclaringType)}{type.Name}";
                    }

                    return jsonTypeInfo.Type.IsNested ? GetFullNestedTypeName(jsonTypeInfo.Type) : OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo); ;
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
}