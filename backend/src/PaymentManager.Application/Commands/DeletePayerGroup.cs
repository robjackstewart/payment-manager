using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Common.Validation;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using DomainValidationException = PaymentManager.Application.Common.Exceptions.ValidationException;

namespace PaymentManager.Application.Commands;

public record DeletePayerGroup(Guid Id) : IRequest
{
    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<DeletePayerGroup, Unit>
    {
        public async Task<Unit> Handle(DeletePayerGroup request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting payer group '{Id}'", request.Id);
            var payerGroup = await context.PayerGroups.FindAsync([request.Id], cancellationToken);

            if (payerGroup is null)
            {
                throw new NotFoundException<PayerGroup>($"Id: {request.Id}");
            }

            // Payment -> PayerGroup is Restrict, so answer with the reason rather than letting the
            // database reject it as an unhandled failure.
            var hasPayments = await context.Payments
                .AnyAsync(p => p.PayerGroupId == payerGroup.Id, cancellationToken);
            if (hasPayments)
            {
                throw new DomainValidationException([
                    new ValidationError
                    {
                        PropertyName = "Id",
                        Errors = ["This payer group still has payments assigned to it. Move or delete those payments first."]
                    }
                ]);
            }

            var memberships = await context.PayerGroupMembers
                .Where(m => m.PayerGroupId == payerGroup.Id)
                .ToListAsync(cancellationToken);
            context.PayerGroupMembers.RemoveRange(memberships);

            context.PayerGroups.Remove(payerGroup);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Deleted payer group '{Id}'", payerGroup.Id);
            return Unit.Value;
        }
    }
}
