using MediatR;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record DeletePayee(Guid Id) : IRequest
{
    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<DeletePayee>
    {
        public async Task Handle(DeletePayee request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting payee '{Id}'", request.Id);
            var payee = await context.Payees.FindAsync([request.Id], cancellationToken);

            if (payee is null)
            {
                throw new NotFoundException<Payee>($"Id: {request.Id}");
            }

            context.Payees.Remove(payee);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Deleted payee '{Id}'", request.Id);
        }
    }
}
