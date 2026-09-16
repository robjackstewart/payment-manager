using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Commands.CreatePayment;

namespace PaymentManager.Application.Commands;

public record CreatePayment(Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, PaymentDirection Direction, DateOnly StartDate, DateOnly? EndDate, string? Description = null, Guid? PayerGroupId = null, IReadOnlyList<CreatePayment.SplitRequest>? Splits = null) : IRequest<Response>
{
    public record SplitRequest(Guid PersonId, decimal Percentage);

    internal sealed class Validator : AbstractValidator<CreatePayment>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.PaymentSourceId).NotEmpty();
            RuleFor(x => x.PayeeId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
            RuleFor(x => x.Frequency).IsInEnum();
            RuleFor(x => x.Direction).IsInEnum();
            RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
            RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
            RuleFor(x => x.EndDate).Must(endDate => endDate is null).When(x => x.Frequency == PaymentFrequency.Once)
                .WithMessage("EndDate must be null when Frequency is Once.");
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
            RuleForEach(x => x.Splits).ChildRules(split =>
            {
                split.RuleFor(s => s.PersonId).NotEmpty();
                split.RuleFor(s => s.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
            }).When(x => x.Splits is { Count: > 0 });
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreatePayment, Response>
    {
        public async Task<Response> Handle(CreatePayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating payment for user: '{UserId}'", request.UserId);

            var splits = request.Splits?.Select(s => (s.PersonId, s.Percentage)).ToArray() ?? [];
            await PaymentSplitGuard.ValidateAsync(
                context, request.UserId, request.Direction, request.PayerGroupId, splits, cancellationToken);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                PaymentSourceId = request.PaymentSourceId,
                PayeeId = request.PayeeId,
                InitialAmount = request.Amount,
                Currency = request.Currency,
                Frequency = request.Frequency,
                Direction = request.Direction,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Description = request.Description,
                PayerGroupId = request.PayerGroupId
            };

            context.Payments.Add(payment);

            foreach (var split in splits)
            {
                context.PaymentSplits.Add(new PaymentSplit
                {
                    PaymentId = payment.Id,
                    PersonId = split.PersonId,
                    Percentage = split.Percentage
                });
            }

            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created payment '{Id}' for user: '{UserId}'", payment.Id, payment.UserId);

            var splitDtos = SplitPaymentCalculator.AllocateValues(request.Amount, splits)
                .Select(s => new Response.SplitDto(s.PersonId, s.Percentage, s.Value))
                .ToArray();

            return new Response(payment.Id, payment.UserId, payment.PaymentSourceId, payment.PayeeId, request.Amount, request.Amount, [], payment.Currency, payment.Frequency, payment.Direction, payment.StartDate, payment.EndDate, payment.Description, payment.PayerGroupId, splitDtos);
        }
    }

    public record Response(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal CurrentAmount, decimal InitialAmount, ICollection<Response.ValueDto> Values, string Currency, PaymentFrequency Frequency, PaymentDirection Direction, DateOnly StartDate, DateOnly? EndDate, string? Description, Guid? PayerGroupId, ICollection<Response.SplitDto> Splits)
    {
        public record ValueDto(DateOnly EffectiveDate, decimal Amount);
        public record SplitDto(Guid PersonId, decimal Percentage, decimal Value);
    }
}
