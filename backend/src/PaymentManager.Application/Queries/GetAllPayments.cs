using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Queries.GetAllPayments;
using static PaymentManager.Application.Queries.GetAllPayments.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPayments(Guid UserId, PaymentDirection? Direction = null) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllPayments, Response>
    {
        public async Task<Response> Handle(GetAllPayments request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all payments for user: '{UserId}'...", request.UserId);

            var payments = await context.Payments
                .Where(x => x.UserId == request.UserId)
                .Where(x => request.Direction == null || x.Direction == request.Direction)
                .ToArrayAsync(cancellationToken);

            var paymentIds = payments.Select(p => p.Id).ToHashSet();

            var splitRows = await context.PaymentSplits
                .Where(s => paymentIds.Contains(s.PaymentId))
                .Select(s => new { s.PaymentId, s.PersonId, s.Percentage })
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
                    var currentAmount = EffectiveValueResolver.Resolve(values, today, p.InitialAmount);
                    var rows = splitRowsByPayment.GetValueOrDefault(p.Id) ?? [];
                    var splitDtos = SplitPaymentCalculator.AllocateValues(
                            currentAmount,
                            rows.Select(s => (s.PersonId, s.Percentage)).ToArray())
                        .Select(s => new PaymentDto.SplitDto(s.PersonId, s.Percentage, s.Value))
                        .ToArray();
                    var valueDtos = values.Select(v => new PaymentDto.ValueDto(v.EffectiveDate, v.Amount)).ToArray();
                    return new PaymentDto(
                        p.Id, p.UserId, p.PaymentSourceId, p.PayeeId,
                        currentAmount, p.InitialAmount, valueDtos, p.Currency, p.Frequency, p.Direction, p.StartDate, p.EndDate, p.Description,
                        p.PayerGroupId, splitDtos);
                })
                .ToArray();

            logger.LogInformation("Successfully fetched {count} payments for user: '{UserId}'", paymentDtos.Length, request.UserId);
            return new Response([.. paymentDtos]);
        }
    }

    public record Response(ICollection<PaymentDto> Payments)
    {
        public record PaymentDto(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal CurrentAmount, decimal InitialAmount, ICollection<PaymentDto.ValueDto> Values, string Currency, PaymentFrequency Frequency, PaymentDirection Direction, DateOnly StartDate, DateOnly? EndDate, string? Description, Guid? PayerGroupId, ICollection<PaymentDto.SplitDto> Splits)
        {
            public record ValueDto(DateOnly EffectiveDate, decimal Amount);
            public record SplitDto(Guid PersonId, decimal Percentage, decimal Value);
        }
    }
}
