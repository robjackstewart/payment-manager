using Microsoft.Extensions.DependencyInjection;
using PaymentManager.Application.Common.Validation;

namespace PaymentManager.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerApplication(this IServiceCollection services)
        => services.AddMediatR(conf =>
        {
            conf.RegisterServicesFromAssemblyContaining<ApplicationAssembly>();
            conf.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
}