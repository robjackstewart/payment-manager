using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record DeletePayment(Guid Id) : IRequest
{
    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<DeletePayment, Unit>
    {
        public async Task<Unit> Handle(DeletePayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting payment '{Id}'", request.Id);
            var payment = await context.Payments.FindAsync([request.Id], cancellationToken);

            if (payment is null)
            {
                throw new NotFoundException<Payment>($"Id: {request.Id}");
            }

            context.Payments.Remove(payment);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Deleted payment '{Id}'", request.Id);
            return Unit.Value;
        }
    }
}
