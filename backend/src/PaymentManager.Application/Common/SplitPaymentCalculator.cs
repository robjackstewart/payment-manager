namespace PaymentManager.Application.Common;

internal static class SplitPaymentCalculator
{
    /// <summary>
    /// Returns the monetary value of a given percentage of an amount, truncated (floored) to
    /// 2 decimal places.
    /// </summary>
    public static decimal CalculateValue(decimal amount, decimal percentage) =>
        Math.Floor(amount * percentage / 100m * 100m) / 100m;

    /// <summary>
    /// Apportions <paramref name="amount"/> across splits whose percentages total 100.
    /// Every share is floored to 2dp and the leftover pennies are given to the largest share,
    /// so the shares always sum to exactly <paramref name="amount"/>. Ties are broken by the
    /// order supplied, which keeps the result deterministic.
    /// </summary>
    public static IReadOnlyList<(Guid PersonId, decimal Percentage, decimal Value)> AllocateValues(
        decimal amount,
        IReadOnlyList<(Guid PersonId, decimal Percentage)> splits)
    {
        if (splits.Count == 0)
        {
            return [];
        }

        var allocated = splits
            .Select(s => (s.PersonId, s.Percentage, Value: CalculateValue(amount, s.Percentage)))
            .ToArray();

        var remainder = amount - allocated.Sum(a => a.Value);
        if (remainder == 0m)
        {
            return allocated;
        }

        var largestIndex = 0;
        for (var i = 1; i < allocated.Length; i++)
        {
            if (allocated[i].Percentage > allocated[largestIndex].Percentage)
            {
                largestIndex = i;
            }
        }

        allocated[largestIndex].Value += remainder;
        return allocated;
    }
}
