using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Commands.UpdatePayment;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record UpdatePayment(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal InitialAmount, string Currency, PaymentFrequency Frequency, PaymentDirection Direction, DateOnly StartDate, DateOnly? EndDate, string? Description = null, Guid? PayerGroupId = null, IReadOnlyList<UpdatePayment.SplitRequest>? Splits = null) : IRequest<Response>
{
    public record SplitRequest(Guid PersonId, decimal Percentage);

    internal sealed class Validator : AbstractValidator<UpdatePayment>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.PaymentSourceId).NotEmpty();
            RuleFor(x => x.PayeeId).NotEmpty();
            RuleFor(x => x.InitialAmount).GreaterThan(0);
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

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdatePayment, Response>
    {
        public async Task<Response> Handle(UpdatePayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating payment '{Id}'", request.Id);
            var payment = await context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (payment is null)
            {
                throw new NotFoundException<Payment>($"Id: {request.Id}");
            }

            var splits = request.Splits?.Select(s => (s.PersonId, s.Percentage)).ToArray() ?? [];
            await PaymentSplitGuard.ValidateAsync(
                context, request.UserId, request.Direction, request.PayerGroupId, splits, cancellationToken);

            var groupChanged = payment.PayerGroupId != request.PayerGroupId;

            payment = payment with
            {
                UserId = request.UserId,
                PaymentSourceId = request.PaymentSourceId,
                PayeeId = request.PayeeId,
                InitialAmount = request.InitialAmount,
                Currency = request.Currency,
                Frequency = request.Frequency,
                Direction = request.Direction,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Description = request.Description,
                PayerGroupId = request.PayerGroupId
            };

            context.Payments.Update(payment);

            var existingSplits = await context.PaymentSplits
                .Where(s => s.PaymentId == payment.Id)
                .ToListAsync(cancellationToken);
            context.PaymentSplits.RemoveRange(existingSplits);

            foreach (var split in splits)
            {
                context.PaymentSplits.Add(new PaymentSplit
                {
                    PaymentId = payment.Id,
                    PersonId = split.PersonId,
                    Percentage = split.Percentage
                });
            }

            // Dated split sets are only valid for outgoing payments inside the payment's current
            // schedule and payer group. Changing direction or group clears them (the UI does the
            // same), and moving the start/end dates drops any sets now outside the schedule.
            var effectiveSplits = await context.EffectivePaymentSplits
                .Where(s => s.PaymentId == payment.Id)
                .ToListAsync(cancellationToken);

            if (request.Direction == PaymentDirection.Incoming || groupChanged)
            {
                context.EffectivePaymentSplits.RemoveRange(effectiveSplits);
                effectiveSplits.Clear();
            }
            else
            {
                var invalid = effectiveSplits
                    .Where(s => s.EffectiveDate <= payment.StartDate
                        || (payment.EndDate.HasValue && s.EffectiveDate > payment.EndDate.Value))
                    .ToList();
                if (invalid.Count > 0)
                {
                    context.EffectivePaymentSplits.RemoveRange(invalid);
                    foreach (var split in invalid)
                        effectiveSplits.Remove(split);
                }
            }

            await context.SaveChanges(cancellationToken);

            var effectiveValues = await context.EffectivePaymentValues
                .Where(v => v.PaymentId == payment.Id)
                .OrderBy(v => v.EffectiveDate)
                .ToArrayAsync(cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentAmount = EffectiveValueResolver.Resolve(effectiveValues, today, payment.InitialAmount);

            logger.LogInformation("Updated payment '{Id}' for user: '{UserId}'", payment.Id, payment.UserId);

            var effectiveSplitRows = effectiveSplits.OrderBy(s => s.EffectiveDate).ToArray();
            var currentSplits = EffectiveSplitResolver.Resolve(splits, effectiveSplitRows, today);
            var splitDtos = SplitPaymentCalculator.AllocateValues(currentAmount, currentSplits)
                .Select(s => new Response.SplitDto(s.PersonId, s.Percentage, s.Value))
                .ToArray();
            var initialSplitDtos = SplitPaymentCalculator.AllocateValues(payment.InitialAmount, splits)
                .Select(s => new Response.SplitDto(s.PersonId, s.Percentage, s.Value))
                .ToArray();
            var splitVersionDtos = EffectiveSplitResolver.GroupVersions(effectiveSplitRows)
                .Select(v => new Response.SplitVersionDto(v.EffectiveDate,
                    [.. v.Splits.Select(s => new Response.SplitVersionDto.SplitDto(s.PersonId, s.Percentage))]))
                .ToArray();
            var valueDtos = effectiveValues.Select(v => new Response.ValueDto(v.EffectiveDate, v.Amount)).ToArray();
            return new Response(payment.Id, payment.UserId, payment.PaymentSourceId, payment.PayeeId, currentAmount, payment.InitialAmount, valueDtos, payment.Currency, payment.Frequency, payment.Direction, payment.StartDate, payment.EndDate, payment.Description, payment.PayerGroupId, splitDtos, initialSplitDtos, splitVersionDtos);
        }
    }

    public record Response(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal CurrentAmount, decimal InitialAmount, ICollection<Response.ValueDto> Values, string Currency, PaymentFrequency Frequency, PaymentDirection Direction, DateOnly StartDate, DateOnly? EndDate, string? Description, Guid? PayerGroupId, ICollection<Response.SplitDto> Splits, ICollection<Response.SplitDto> InitialSplits, ICollection<Response.SplitVersionDto> SplitVersions)
    {
        public record ValueDto(DateOnly EffectiveDate, decimal Amount);
        public record SplitDto(Guid PersonId, decimal Percentage, decimal Value);
        public record SplitVersionDto(DateOnly EffectiveDate, ICollection<SplitVersionDto.SplitDto> Splits)
        {
            public record SplitDto(Guid PersonId, decimal Percentage);
        }
    }
}
