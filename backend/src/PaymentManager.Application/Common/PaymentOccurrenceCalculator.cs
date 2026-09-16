using PaymentManager.Domain.Enums;

namespace PaymentManager.Application.Common;

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
