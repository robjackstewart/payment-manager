using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Common.Validation;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Commands.AddPaymentSplits;
using static PaymentManager.Application.Common.Exceptions;
using DomainValidationException = PaymentManager.Application.Common.Exceptions.ValidationException;

namespace PaymentManager.Application.Commands;

/// <summary>
/// Adds (or replaces) the split set that takes effect on <paramref name="EffectiveDate"/> for a
/// payment. Only outgoing payments may carry dated split sets — income always belongs to a single
/// person. The set is validated by <see cref="PaymentSplitGuard"/> exactly like the initial split.
/// </summary>
public record AddPaymentSplits(Guid PaymentId, DateOnly EffectiveDate, IReadOnlyList<SplitRequest> Splits) : IRequest<Response>
{
    public record SplitRequest(Guid PersonId, decimal Percentage);

    internal sealed class Validator : AbstractValidator<AddPaymentSplits>
    {
        public Validator()
        {
            RuleFor(x => x.PaymentId).NotEmpty();
            RuleFor(x => x.EffectiveDate).NotEqual(default(DateOnly));
            RuleFor(x => x.Splits).NotEmpty();
            RuleForEach(x => x.Splits).ChildRules(split =>
            {
                split.RuleFor(s => s.PersonId).NotEmpty();
                split.RuleFor(s => s.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
            });
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<AddPaymentSplits, Response>
    {
        public async Task<Response> Handle(AddPaymentSplits request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Adding effective splits for payment '{PaymentId}' effective {EffectiveDate}", request.PaymentId, request.EffectiveDate);

            var payment = await context.Payments.FindAsync([request.PaymentId], cancellationToken);
            if (payment is null)
            {
                throw new NotFoundException<Payment>($"Id: {request.PaymentId}");
            }

            List<ValidationError> errors = [];
            if (payment.Direction != PaymentDirection.Outgoing)
                errors.Add(new ValidationError { PropertyName = nameof(EffectiveDate), Errors = ["Effective-dated splits only apply to outgoing payments."] });
            if (request.EffectiveDate <= payment.StartDate)
                errors.Add(new ValidationError { PropertyName = nameof(EffectiveDate), Errors = [$"Effective date must be after the payment start date ({payment.StartDate})."] });
            if (payment.EndDate.HasValue && request.EffectiveDate > payment.EndDate.Value)
                errors.Add(new ValidationError { PropertyName = nameof(EffectiveDate), Errors = [$"Effective date cannot be later than the payment end date ({payment.EndDate.Value})."] });
            if (errors.Count > 0)
                throw new DomainValidationException(errors);

            var splits = request.Splits.Select(s => (s.PersonId, s.Percentage)).ToArray();
            await PaymentSplitGuard.ValidateAsync(
                context, payment.UserId, payment.Direction, payment.PayerGroupId, splits, cancellationToken);

            var existing = await context.EffectivePaymentSplits
                .Where(s => s.PaymentId == request.PaymentId && s.EffectiveDate == request.EffectiveDate)
                .ToListAsync(cancellationToken);
            context.EffectivePaymentSplits.RemoveRange(existing);

            foreach (var split in splits)
            {
                context.EffectivePaymentSplits.Add(new EffectivePaymentSplit
                {
                    PaymentId = request.PaymentId,
                    EffectiveDate = request.EffectiveDate,
                    PersonId = split.PersonId,
                    Percentage = split.Percentage
                });
            }

            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Added effective splits for payment '{PaymentId}' effective {EffectiveDate}", request.PaymentId, request.EffectiveDate);

            return new Response(request.PaymentId, request.EffectiveDate,
                [.. splits.Select(s => new Response.SplitDto(s.PersonId, s.Percentage))]);
        }
    }

    public record Response(Guid PaymentId, DateOnly EffectiveDate, ICollection<Response.SplitDto> Splits)
    {
        public record SplitDto(Guid PersonId, decimal Percentage);
    }
}
