using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Queries.GetAllPayments;
using static PaymentManager.Application.Queries.GetAllPayments.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPayments(Guid UserId) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllPayments, Response>
    {
        public async Task<Response> Handle(GetAllPayments request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all payments for user: '{UserId}'...", request.UserId);

            var payments = await context.Payments
                .Where(x => x.UserId == request.UserId)
                .ToArrayAsync(cancellationToken);

            var paymentIds = payments.Select(p => p.Id).ToHashSet();

            var splitRows = await context.PaymentSplits
                .Where(s => paymentIds.Contains(s.PaymentId))
                .Select(s => new { s.PaymentId, s.ContactId, s.Percentage })
                .ToArrayAsync(cancellationToken);

            var splitRowsByPayment = splitRows
                .GroupBy(s => s.PaymentId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            var effectiveValueRows = await context.EffectivePaymentValues
                .Where(v => paymentIds.Contains(v.PaymentId))
                .OrderBy(v => v.EffectiveDate)
                .ToArrayAsync(cancellationToken);

            var effectiveValuesByPayment = effectiveValueRows
                .GroupBy(v => v.PaymentId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var paymentDtos = payments
                .OrderBy(p => p.Id)
                .Select(p =>
                {
                    var values = effectiveValuesByPayment.GetValueOrDefault(p.Id) ?? [];
                    var currentAmount = values.Where(v => v.EffectiveDate <= today).LastOrDefault()?.Amount
                        ?? p.InitialAmount;
                    var rows = splitRowsByPayment.GetValueOrDefault(p.Id) ?? [];
                    var splitDtos = rows
                        .Select(s => new PaymentDto.SplitDto(s.ContactId, s.Percentage,
                            SplitPaymentCalculator.CalculateValue(currentAmount, s.Percentage)))
                        .ToArray();
                    var userSharePct = SplitPaymentCalculator.UserSharePercentage(splitDtos.Select(s => s.Percentage));
                    var userShare = new UserShareDto(userSharePct, SplitPaymentCalculator.UserShareValue(currentAmount, splitDtos.Select(s => s.Value)));
                    var valueDtos = values.Select(v => new PaymentDto.ValueDto(v.EffectiveDate, v.Amount)).ToArray();
                    return new PaymentDto(
                        p.Id, p.UserId, p.PaymentSourceId, p.PayeeId,
                        currentAmount, p.InitialAmount, valueDtos, p.Currency, p.Frequency, p.StartDate, p.EndDate, p.Description,
                        userShare, splitDtos);
                })
                .ToArray();

            logger.LogInformation("Successfully fetched {count} payments for user: '{UserId}'", paymentDtos.Length, request.UserId);
            return new Response([.. paymentDtos]);
        }
    }

    public record Response(ICollection<PaymentDto> Payments)
    {
        public record PaymentDto(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal CurrentAmount, decimal InitialAmount, ICollection<PaymentDto.ValueDto> Values, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description, UserShareDto UserShare, ICollection<PaymentDto.SplitDto> Splits)
        {
            public record ValueDto(DateOnly EffectiveDate, decimal Amount);
            public record SplitDto(Guid ContactId, decimal Percentage, decimal Value);
        }
    }
}
