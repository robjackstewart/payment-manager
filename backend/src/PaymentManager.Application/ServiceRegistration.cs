using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PaymentManager.Application.Commands;
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
        => services.AddTransient<IValidator<CreateUser>, CreateUser.Validator>();
}