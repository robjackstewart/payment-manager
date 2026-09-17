using PaymentManager.Domain.Entities;

namespace PaymentManager.Application.Common;

internal static class EffectiveSplitResolver
{
    /// <summary>
    /// Resolves the split set in effect on a given date: the rows sharing the most recent
    /// <see cref="EffectivePaymentSplit.EffectiveDate"/> on or before <paramref name="asOfDate"/>,
    /// or <paramref name="initialSplits"/> when none apply yet. <paramref name="sortedEffectiveSplits"/>
    /// must be sorted by <see cref="EffectivePaymentSplit.EffectiveDate"/> ascending.
    /// </summary>
    public static IReadOnlyList<(Guid PersonId, decimal Percentage)> Resolve(
        IReadOnlyList<(Guid PersonId, decimal Percentage)> initialSplits,
        IReadOnlyList<EffectivePaymentSplit> sortedEffectiveSplits,
        DateOnly asOfDate)
    {
        DateOnly? current = null;
        foreach (var split in sortedEffectiveSplits)
        {
            if (split.EffectiveDate <= asOfDate)
                current = split.EffectiveDate;
            else
                break;
        }

        if (current is null)
            return initialSplits;

        return
        [
            .. sortedEffectiveSplits
                .Where(s => s.EffectiveDate == current.Value)
                .Select(s => (s.PersonId, s.Percentage))
        ];
    }

    /// <summary>
    /// Groups a payment's effective split rows into one entry per effective date, so each entry is a
    /// complete split set. Input must be sorted by effective date ascending.
    /// </summary>
    public static IReadOnlyList<(DateOnly EffectiveDate, IReadOnlyList<(Guid PersonId, decimal Percentage)> Splits)> GroupVersions(
        IReadOnlyList<EffectivePaymentSplit> sortedEffectiveSplits) =>
        [
            .. sortedEffectiveSplits
                .GroupBy(s => s.EffectiveDate)
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, (IReadOnlyList<(Guid PersonId, decimal Percentage)>)[.. g.Select(s => (s.PersonId, s.Percentage))]))
        ];
}
