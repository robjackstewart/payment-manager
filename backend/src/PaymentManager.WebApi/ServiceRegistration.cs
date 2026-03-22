using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.OpenApi;
using PaymentManager.Application;
using PaymentManager.Infrastructure;
using PaymentManager.WebApi.Endpoints;
using PaymentManager.WebApi.Services;

namespace PaymentManager.WebApi;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerWebApi(this IServiceCollection services, Configuration configuration)
    {
        services
        .AddScoped<IUserService, DefaultUserService>()
        .AddPaymentManagerApplication()
        .AddPaymentManagerInfrastructure(new Infrastructure.Configuration
        {
            DatabaseConnectionString = configuration.ConnectionStrings.PaymentManager
        })
        .AddOpenApi(opt =>
        {
            opt.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
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
            opt.AddPolicy(Constants.Cors.ALLOW_ALL_POLICY_NAME, policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }
}