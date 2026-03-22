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

            var paymentDtos = payments
                .OrderBy(p => p.Id)
                .Select(p =>
                {
                    var rows = splitRowsByPayment.GetValueOrDefault(p.Id) ?? [];
                    var splitDtos = rows
                        .Select(s => new PaymentDto.SplitDto(s.ContactId, s.Percentage,
                            SplitPaymentCalculator.CalculateValue(p.Amount, s.Percentage)))
                        .ToArray();
                    var userSharePct = SplitPaymentCalculator.UserSharePercentage(splitDtos.Select(s => s.Percentage));
                    var userShare = new UserShareDto(userSharePct, SplitPaymentCalculator.CalculateValue(p.Amount, userSharePct));
                    return new PaymentDto(
                        p.Id, p.UserId, p.PaymentSourceId, p.PayeeId,
                        p.Amount, p.Currency, p.Frequency, p.StartDate, p.EndDate, p.Description,
                        userShare, splitDtos);
                })
                .ToArray();

            logger.LogInformation("Successfully fetched {count} payments for user: '{UserId}'", paymentDtos.Length, request.UserId);
            return new Response([.. paymentDtos]);
        }
    }

    public record Response(ICollection<PaymentDto> Payments)
    {
        public record PaymentDto(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description, UserShareDto UserShare, ICollection<PaymentDto.SplitDto> Splits)
        {
            public record SplitDto(Guid ContactId, decimal Percentage, decimal Value);
        }
    }
}
