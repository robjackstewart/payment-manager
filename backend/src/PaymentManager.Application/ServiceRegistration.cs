using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PaymentManager.Application.Common.Validation;

namespace PaymentManager.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerApplication(this IServiceCollection services)
        => services.AddValidators()
            .AddMediatR(conf =>
        {
            conf.RegisterServicesFromAssemblyContaining<ApplicationAssembly>();
            conf.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationAssembly).Assembly;
        var validatorType = typeof(IValidator<>);

        var validators = assembly.GetTypes()
            .Where(t => !t.IsAbstract && (t.IsPublic || t.IsNestedPublic || t.IsNestedFamily || t.IsNestedFamORAssem || t.IsNestedAssembly))
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType))
            .ToList();

        foreach (var validator in validators)
        {
            var interfaces = validator.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                services.AddTransient(@interface, validator);
            }
        }

        return services;
    }
}