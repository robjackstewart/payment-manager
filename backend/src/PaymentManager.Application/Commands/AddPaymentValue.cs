using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Validation;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.AddPaymentValue;
using static PaymentManager.Application.Common.Exceptions;
using DomainValidationException = PaymentManager.Application.Common.Exceptions.ValidationException;

namespace PaymentManager.Application.Commands;

public record AddPaymentValue(Guid PaymentId, DateOnly EffectiveDate, decimal Amount) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<AddPaymentValue>
    {
        public Validator()
        {
            RuleFor(x => x.PaymentId).NotEmpty();
            RuleFor(x => x.EffectiveDate).NotEqual(default(DateOnly));
            RuleFor(x => x.Amount).GreaterThan(0);
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<AddPaymentValue, Response>
    {
        public async Task<Response> Handle(AddPaymentValue request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Adding effective value for payment '{PaymentId}' effective {EffectiveDate}", request.PaymentId, request.EffectiveDate);

            var payment = await context.Payments.FindAsync([request.PaymentId], cancellationToken);
            if (payment is null)
            {
                throw new NotFoundException<Payment>($"Id: {request.PaymentId}");
            }

            List<ValidationError> errors = [];
            if (request.EffectiveDate <= payment.StartDate)
                errors.Add(new ValidationError { PropertyName = nameof(EffectiveDate), Errors = [$"Effective date must be after the payment start date ({payment.StartDate})."] });
            if (payment.EndDate.HasValue && request.EffectiveDate > payment.EndDate.Value)
                errors.Add(new ValidationError { PropertyName = nameof(EffectiveDate), Errors = [$"Effective date cannot be later than the payment end date ({payment.EndDate.Value})."] });
            if (errors.Count > 0)
                throw new DomainValidationException(errors);

            var existing = await context.EffectivePaymentValues
                .FirstOrDefaultAsync(v => v.PaymentId == request.PaymentId && v.EffectiveDate == request.EffectiveDate, cancellationToken);

            EffectivePaymentValue value;
            if (existing is not null)
            {
                value = existing with { Amount = request.Amount };
                context.EffectivePaymentValues.Remove(existing);
                context.EffectivePaymentValues.Add(value);
            }
            else
            {
                value = new EffectivePaymentValue
                {
                    Id = Guid.NewGuid(),
                    PaymentId = request.PaymentId,
                    EffectiveDate = request.EffectiveDate,
                    Amount = request.Amount
                };
                context.EffectivePaymentValues.Add(value);
            }
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Added effective value '{Id}' for payment '{PaymentId}'", value.Id, value.PaymentId);

            return new Response(value.Id, value.PaymentId, value.EffectiveDate, value.Amount);
        }
    }

    public record Response(Guid Id, Guid PaymentId, DateOnly EffectiveDate, decimal Amount);
}
