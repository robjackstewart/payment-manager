namespace PaymentManager.Application.Common;

public record UserShareDto(decimal Percentage, decimal Value);

internal static class SplitPaymentCalculator
{
    /// <summary>Returns the owner's share percentage: 100 minus the sum of all contact split percentages.</summary>
    public static decimal UserSharePercentage(IEnumerable<decimal> contactSplitPercentages) =>
        100m - contactSplitPercentages.Sum();

    /// <summary>Returns the monetary value of a given percentage of an amount, rounded to 2 decimal places.</summary>
    public static decimal CalculateValue(decimal amount, decimal percentage) =>
        Math.Round(amount * percentage / 100m, 2);
}
