using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Queries.GetPaymentOccurrences;
using static PaymentManager.Application.Queries.GetPaymentOccurrences.Response;

namespace PaymentManager.Application.Queries;

public record GetPaymentOccurrences(Guid UserId, DateOnly From, DateOnly To) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger)
        : IRequestHandler<GetPaymentOccurrences, Response>
    {
        public async Task<Response> Handle(GetPaymentOccurrences request, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Fetching payment occurrences for user '{UserId}' between {From} and {To}",
                request.UserId, request.From, request.To);

            var payments = await context.Payments
                .Where(x => x.UserId == request.UserId
                    && x.StartDate <= request.To
                    && (x.EndDate == null || x.EndDate >= request.From))
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
                .Where(v => paymentIds.Contains(v.PaymentId) && v.EffectiveDate <= request.To)
                .OrderBy(v => v.EffectiveDate)
                .ToArrayAsync(cancellationToken);

            var effectiveValuesByPayment = effectiveValueRows
                .GroupBy(v => v.PaymentId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            var occurrences = payments
                .SelectMany(p =>
                {
                    var rows = splitRowsByPayment.GetValueOrDefault(p.Id) ?? [];
                    var effectiveValues = effectiveValuesByPayment.GetValueOrDefault(p.Id) ?? [];
                    return PaymentOccurrenceCalculator
                        .GetOccurrences(p.Frequency, p.StartDate, p.EndDate, request.From, request.To)
                        .Select(date =>
                        {
                            var amount = ResolveEffectiveAmount(effectiveValues, date, p.InitialAmount);
                            var splitDtos = (ICollection<OccurrenceDto.SplitDto>)rows
                                .Select(s => new OccurrenceDto.SplitDto(s.ContactId, s.Percentage,
                                    SplitPaymentCalculator.CalculateValue(amount, s.Percentage)))
                                .ToArray();
                            var userSharePct = SplitPaymentCalculator.UserSharePercentage(splitDtos.Select(s => s.Percentage));
                            var userShare = new UserShareDto(userSharePct, SplitPaymentCalculator.UserShareValue(amount, splitDtos.Select(s => s.Value)));
                            return new OccurrenceDto(
                                p.Id, p.PaymentSourceId, p.PayeeId,
                                amount, p.Currency, p.Frequency,
                                date, p.StartDate, p.EndDate, p.Description,
                                userShare, splitDtos);
                        });
                })
                .OrderBy(o => o.OccurrenceDate)
                .ThenBy(o => o.PaymentId)
                .ToArray();

            var summary = occurrences
                .GroupBy(o => o.Currency)
                .Select(currencyGroup => new SummaryDto(
                    currencyGroup.Key,
                    currencyGroup.Sum(o => o.Amount),
                    currencyGroup.Sum(o => o.UserShare.Value),
                    [.. currencyGroup
                        .SelectMany(o => o.Splits)
                        .GroupBy(s => s.ContactId)
                        .Select(g => new ContactAmountDto(g.Key, g.Sum(s => s.Value)))
                        .OrderBy(c => c.ContactId)],
                    [.. currencyGroup
                        .GroupBy(o => o.PaymentSourceId)
                        .Select(psGroup => new PaymentSourceBreakdownDto(
                            psGroup.Key,
                            psGroup.Sum(o => o.Amount),
                            psGroup.Sum(o => o.UserShare.Value),
                            [.. psGroup
                                .SelectMany(o => o.Splits)
                                .GroupBy(s => s.ContactId)
                                .Select(g => new ContactAmountDto(g.Key, g.Sum(s => s.Value)))
                                .OrderBy(c => c.ContactId)]))
                        .OrderBy(ps => ps.PaymentSourceId)]))
                .OrderBy(s => s.Currency)
                .ToArray();

            logger.LogInformation(
                "Found {Count} occurrences for user '{UserId}' between {From} and {To}",
                occurrences.Length, request.UserId, request.From, request.To);

            return new Response([.. occurrences], [.. summary]);
        }

        private static decimal ResolveEffectiveAmount(EffectivePaymentValue[] sortedValues, DateOnly occurrenceDate, decimal initialAmount)
        {
            EffectivePaymentValue? current = null;
            foreach (var v in sortedValues)
            {
                if (v.EffectiveDate <= occurrenceDate)
                    current = v;
                else
                    break;
            }
            return current?.Amount ?? initialAmount;
        }
    }

    public record Response(ICollection<OccurrenceDto> Occurrences, ICollection<SummaryDto> Summary)
    {
        public record OccurrenceDto(
            Guid PaymentId,
            Guid PaymentSourceId,
            Guid PayeeId,
            decimal Amount,
            string Currency,
            PaymentFrequency Frequency,
            DateOnly OccurrenceDate,
            DateOnly StartDate,
            DateOnly? EndDate,
            string? Description,
            UserShareDto UserShare,
            ICollection<OccurrenceDto.SplitDto> Splits)
        {
            public record SplitDto(Guid ContactId, decimal Percentage, decimal Value);
        }

        public record SummaryDto(
            string Currency,
            decimal TotalAmount,
            decimal UserTotal,
            ICollection<ContactAmountDto> ContactTotals,
            ICollection<PaymentSourceBreakdownDto> ByPaymentSource);

        public record ContactAmountDto(Guid ContactId, decimal Amount);

        public record PaymentSourceBreakdownDto(
            Guid PaymentSourceId,
            decimal TotalAmount,
            decimal UserTotal,
            ICollection<ContactAmountDto> ContactTotals);
    }
}

internal static class PaymentOccurrenceCalculator
{
    public static IEnumerable<DateOnly> GetOccurrences(
        PaymentFrequency frequency,
        DateOnly startDate,
        DateOnly? endDate,
        DateOnly from,
        DateOnly to)
    {
        return frequency switch
        {
            PaymentFrequency.Once => GetOnceOccurrences(startDate, from, to),
            PaymentFrequency.Monthly => GetMonthlyOccurrences(startDate, endDate, from, to),
            PaymentFrequency.Annually => GetAnnualOccurrences(startDate, endDate, from, to),
            _ => []
        };
    }

    private static IEnumerable<DateOnly> GetOnceOccurrences(DateOnly startDate, DateOnly from, DateOnly to)
    {
        if (startDate >= from && startDate <= to)
            yield return startDate;
    }

    private static IEnumerable<DateOnly> GetMonthlyOccurrences(
        DateOnly startDate, DateOnly? endDate, DateOnly from, DateOnly to)
    {
        var current = new DateOnly(from.Year, from.Month, 1);
        var lastMonth = new DateOnly(to.Year, to.Month, 1);

        while (current <= lastMonth)
        {
            var daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);
            var monthStart = current;
            var monthEnd = new DateOnly(current.Year, current.Month, daysInMonth);

            if (startDate <= monthEnd && (endDate == null || endDate >= monthStart))
            {
                var day = Math.Min(startDate.Day, daysInMonth);
                var occurrenceDate = new DateOnly(current.Year, current.Month, day);

                if (occurrenceDate >= from && occurrenceDate <= to && occurrenceDate >= startDate)
                    yield return occurrenceDate;
            }

            current = current.AddMonths(1);
        }
    }

    private static IEnumerable<DateOnly> GetAnnualOccurrences(
        DateOnly startDate, DateOnly? endDate, DateOnly from, DateOnly to)
    {
        for (var year = from.Year; year <= to.Year; year++)
        {
            var daysInMonth = DateTime.DaysInMonth(year, startDate.Month);
            var day = Math.Min(startDate.Day, daysInMonth);
            var occurrenceDate = new DateOnly(year, startDate.Month, day);

            if (occurrenceDate >= from && occurrenceDate <= to
                && occurrenceDate >= startDate
                && (endDate == null || occurrenceDate <= endDate))
                yield return occurrenceDate;
        }
    }
}
