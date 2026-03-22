namespace PaymentManager.Application.Common;

public record UserShareDto(decimal Percentage, decimal Value);

internal static class SplitPaymentCalculator
{
    /// <summary>Returns the owner's share percentage: 100 minus the sum of all contact split percentages.</summary>
    public static decimal UserSharePercentage(IEnumerable<decimal> contactSplitPercentages) =>
        100m - contactSplitPercentages.Sum();

    /// <summary>
    /// Returns the owner's share value as the remainder after subtracting all contact split values from
    /// the total amount. This guarantees the shares always sum to exactly the original amount, regardless
    /// of rounding on individual contact splits.
    /// </summary>
    public static decimal UserShareValue(decimal amount, IEnumerable<decimal> contactSplitValues) =>
        amount - contactSplitValues.Sum();

    /// <summary>
    /// Returns the monetary value of a given percentage of an amount, truncated (floored) to 2 decimal
    /// places. Truncation ensures contacts never round up, so the user (bill owner) absorbs any
    /// sub-penny remainder via <see cref="UserShareValue"/>.
    /// </summary>
    public static decimal CalculateValue(decimal amount, decimal percentage) =>
        Math.Floor(amount * percentage / 100m * 100m) / 100m;
}
