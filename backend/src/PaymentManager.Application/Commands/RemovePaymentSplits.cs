using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record RemovePaymentSplits(Guid PaymentId, DateOnly EffectiveDate) : IRequest
{
    internal sealed class Validator : AbstractValidator<RemovePaymentSplits>
    {
        public Validator()
        {
            RuleFor(x => x.PaymentId).NotEmpty();
            RuleFor(x => x.EffectiveDate).NotEqual(default(DateOnly));
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<RemovePaymentSplits, Unit>
    {
        public async Task<Unit> Handle(RemovePaymentSplits request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Removing effective splits for payment '{PaymentId}' effective {EffectiveDate}", request.PaymentId, request.EffectiveDate);

            var payment = await context.Payments.FindAsync([request.PaymentId], cancellationToken);
            if (payment is null)
                throw new NotFoundException<Payment>($"Id: {request.PaymentId}");

            var splits = await context.EffectivePaymentSplits
                .Where(s => s.PaymentId == request.PaymentId && s.EffectiveDate == request.EffectiveDate)
                .ToListAsync(cancellationToken);
            if (splits.Count == 0)
                throw new NotFoundException<EffectivePaymentSplit>($"PaymentId: {request.PaymentId}, EffectiveDate: {request.EffectiveDate}");

            context.EffectivePaymentSplits.RemoveRange(splits);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Removed effective splits for payment '{PaymentId}' effective {EffectiveDate}", request.PaymentId, request.EffectiveDate);
            return Unit.Value;
        }
    }
}
