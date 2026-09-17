using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Queries.GetPayment;

namespace PaymentManager.Application.Queries;

public record GetPayment(Guid Id) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetPayment, Response>
    {
        public async Task<Response> Handle(GetPayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching payment with id: '{Id}'", request.Id);
            var payment = await context.Payments.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (payment is null)
            {
                throw new NotFoundException<Payment>($"Id: {request.Id}");
            }

            var effectiveValues = await context.EffectivePaymentValues
                .Where(v => v.PaymentId == request.Id)
                .OrderBy(v => v.EffectiveDate)
                .ToArrayAsync(cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentAmount = EffectiveValueResolver.Resolve(effectiveValues, today, payment.InitialAmount);

            var splitRows = await context.PaymentSplits
                .Where(s => s.PaymentId == request.Id)
                .Select(s => new { s.PersonId, s.Percentage })
                .ToArrayAsync(cancellationToken);

            var effectiveSplitRows = await context.EffectivePaymentSplits
                .Where(s => s.PaymentId == request.Id)
                .OrderBy(s => s.EffectiveDate)
                .ToArrayAsync(cancellationToken);

            var initialSplits = splitRows.Select(s => (s.PersonId, s.Percentage)).ToArray();
            var currentSplits = EffectiveSplitResolver.Resolve(initialSplits, effectiveSplitRows, today);

            var splits = SplitPaymentCalculator.AllocateValues(currentAmount, currentSplits)
                .Select(s => new Response.SplitDto(s.PersonId, s.Percentage, s.Value))
                .ToArray();
            var initialSplitDtos = SplitPaymentCalculator.AllocateValues(payment.InitialAmount, initialSplits)
                .Select(s => new Response.SplitDto(s.PersonId, s.Percentage, s.Value))
                .ToArray();
            var splitVersionDtos = EffectiveSplitResolver.GroupVersions(effectiveSplitRows)
                .Select(v => new Response.SplitVersionDto(v.EffectiveDate,
                    [.. v.Splits.Select(s => new Response.SplitVersionDto.SplitDto(s.PersonId, s.Percentage))]))
                .ToArray();

            var valueDtos = effectiveValues.Select(v => new Response.ValueDto(v.EffectiveDate, v.Amount)).ToArray();

            logger.LogInformation("Successfully fetched payment '{Id}'", payment.Id);
            return new Response(payment.Id, payment.UserId, payment.PaymentSourceId, payment.PayeeId, currentAmount, payment.InitialAmount, valueDtos, payment.Currency, payment.Frequency, payment.Direction, payment.StartDate, payment.EndDate, payment.Description, payment.PayerGroupId, splits, initialSplitDtos, splitVersionDtos);
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
