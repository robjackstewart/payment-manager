using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
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
                .Where(x => x.UserId == request.UserId)
                .ToArrayAsync(cancellationToken);

            var paymentIds = payments.Select(p => p.Id).ToHashSet();

            var splitRows = await context.PaymentSplits
                .Where(s => paymentIds.Contains(s.PaymentId))
                .Join(context.Contacts, s => s.ContactId, c => c.Id,
                    (s, c) => new { s.PaymentId, s.ContactId, c.Name, s.Percentage })
                .ToListAsync(cancellationToken);

            var splitsByPayment = splitRows
                .GroupBy(s => s.PaymentId)
                .ToDictionary(
                    g => g.Key,
                    g => (ICollection<OccurrenceDto.SplitDto>)g.Select(s => new OccurrenceDto.SplitDto(s.ContactId, s.Name, s.Percentage)).ToList());

            var occurrences = payments
                .SelectMany(p =>
                {
                    var splits = splitsByPayment.GetValueOrDefault(p.Id) ?? [];
                    return PaymentOccurrenceCalculator
                        .GetOccurrences(p.Frequency, p.StartDate, p.EndDate, request.From, request.To)
                        .Select(date => new OccurrenceDto(
                            p.Id, p.PaymentSourceId, p.PayeeId,
                            p.Amount, p.Currency, p.Frequency,
                            date, p.StartDate, p.EndDate, p.Description, splits));
                })
                .OrderBy(o => o.OccurrenceDate)
                .ThenBy(o => o.PaymentId)
                .ToArray();

            logger.LogInformation(
                "Found {Count} occurrences for user '{UserId}' between {From} and {To}",
                occurrences.Length, request.UserId, request.From, request.To);

            return new Response([.. occurrences]);
        }
    }

    public record Response(ICollection<OccurrenceDto> Occurrences)
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
            ICollection<OccurrenceDto.SplitDto> Splits)
        {
            public record SplitDto(Guid ContactId, string ContactName, decimal Percentage);
        }
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
