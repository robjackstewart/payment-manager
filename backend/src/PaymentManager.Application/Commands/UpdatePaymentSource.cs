using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Commands.UpdatePaymentSource;

namespace PaymentManager.Application.Commands;

public record UpdatePaymentSource(Guid Id, Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<UpdatePaymentSource>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdatePaymentSource, Response>
    {
        public async Task<Response> Handle(UpdatePaymentSource request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating payment source '{Id}'", request.Id);
            var paymentSource = await context.PaymentSources.FindAsync([request.Id], cancellationToken);

            if (paymentSource is null)
            {
                throw new NotFoundException<PaymentSource>($"Id: {request.Id}");
            }

            paymentSource = paymentSource with
            {
                UserId = request.UserId,
                Name = request.Name
            };

            context.PaymentSources.Update(paymentSource);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Updated payment source '{Id}' with name: '{Name}' for user: '{UserId}'", paymentSource.Id, paymentSource.Name, paymentSource.UserId);

            return new Response(paymentSource.Id, paymentSource.UserId, paymentSource.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
