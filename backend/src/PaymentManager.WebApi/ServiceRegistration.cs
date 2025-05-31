using System.Reflection;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.OpenApi;
using PaymentManager.Application;
using PaymentManager.Infrastructure;

namespace PaymentManager.WebApi;

public static class ServiceRegistration
{
    private static readonly bool IsOpenApiExecution = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
    public static IServiceCollection AddPaymentManagerWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredAllowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
        Guard.IsNotNull(configuredAllowedOrigins);

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
                        .WithOrigins(configuredAllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

        return services;
    }
}