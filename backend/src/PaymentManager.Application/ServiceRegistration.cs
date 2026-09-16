using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Common.Validation;

namespace PaymentManager.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentManagerApplication(this IServiceCollection services)
        => services
            .AddScoped<ISender, Dispatcher>()
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
            .Scan(scan => scan
                .FromAssemblyOf<ApplicationAssembly>()
                .AddClasses(c => c.AssignableTo(typeof(IRequestHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime())
            .AddValidators();

    private static IServiceCollection AddValidators(this IServiceCollection services)
        => services
            .AddTransient<IValidator<CreateUser>, CreateUser.Validator>()
            .AddTransient<IValidator<UpdateUser>, UpdateUser.Validator>()
            .AddTransient<IValidator<CreatePaymentSource>, CreatePaymentSource.Validator>()
            .AddTransient<IValidator<UpdatePaymentSource>, UpdatePaymentSource.Validator>()
            .AddTransient<IValidator<CreatePayee>, CreatePayee.Validator>()
            .AddTransient<IValidator<UpdatePayee>, UpdatePayee.Validator>()
            .AddTransient<IValidator<CreatePerson>, CreatePerson.Validator>()
            .AddTransient<IValidator<UpdatePerson>, UpdatePerson.Validator>()
            .AddTransient<IValidator<CreatePayerGroup>, CreatePayerGroup.Validator>()
            .AddTransient<IValidator<UpdatePayerGroup>, UpdatePayerGroup.Validator>()
            .AddTransient<IValidator<SetPayerGroupMembers>, SetPayerGroupMembers.Validator>()
            .AddTransient<IValidator<CreatePayment>, CreatePayment.Validator>()
            .AddTransient<IValidator<UpdatePayment>, UpdatePayment.Validator>();
}