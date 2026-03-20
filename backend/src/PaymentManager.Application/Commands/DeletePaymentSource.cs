using MediatR;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record DeletePaymentSource(Guid Id) : IRequest
{
    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<DeletePaymentSource>
    {
        public async Task Handle(DeletePaymentSource request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting payment source '{Id}'", request.Id);
            var paymentSource = await context.PaymentSources.FindAsync([request.Id], cancellationToken);

            if (paymentSource is null)
            {
                throw new NotFoundException<PaymentSource>($"Id: {request.Id}");
            }

            context.PaymentSources.Remove(paymentSource);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Deleted payment source '{Id}'", request.Id);
        }
    }
}
