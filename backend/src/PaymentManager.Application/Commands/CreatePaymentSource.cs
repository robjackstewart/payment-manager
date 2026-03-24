using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreatePaymentSource;

namespace PaymentManager.Application.Commands;

public record CreatePaymentSource(Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreatePaymentSource>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreatePaymentSource, Response>
    {
        public async Task<Response> Handle(CreatePaymentSource request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating payment source with name: '{Name}' for user: '{UserId}'", request.Name, request.UserId);
            var paymentSource = new PaymentSource
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Name = request.Name
            };

            context.PaymentSources.Add(paymentSource);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created payment source '{Id}' with name: '{Name}' for user: '{UserId}'", paymentSource.Id, paymentSource.Name, paymentSource.UserId);

            return new Response(paymentSource.Id, paymentSource.UserId, paymentSource.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
