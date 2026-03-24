using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record RemovePaymentValue(Guid PaymentId, DateOnly EffectiveDate) : IRequest
{
    internal sealed class Validator : AbstractValidator<RemovePaymentValue>
    {
        public Validator()
        {
            RuleFor(x => x.PaymentId).NotEmpty();
            RuleFor(x => x.EffectiveDate).NotEqual(default(DateOnly));
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<RemovePaymentValue, Unit>
    {
        public async Task<Unit> Handle(RemovePaymentValue request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Removing effective value for payment '{PaymentId}' effective {EffectiveDate}", request.PaymentId, request.EffectiveDate);

            var payment = await context.Payments.FindAsync([request.PaymentId], cancellationToken);
            if (payment is null)
                throw new NotFoundException<Payment>($"Id: {request.PaymentId}");

            var value = await context.EffectivePaymentValues
                .FirstOrDefaultAsync(v => v.PaymentId == request.PaymentId && v.EffectiveDate == request.EffectiveDate, cancellationToken);
            if (value is null)
                throw new NotFoundException<EffectivePaymentValue>($"PaymentId: {request.PaymentId}, EffectiveDate: {request.EffectiveDate}");

            context.EffectivePaymentValues.Remove(value);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Removed effective value for payment '{PaymentId}' effective {EffectiveDate}", request.PaymentId, request.EffectiveDate);
            return Unit.Value;
        }
    }
}
