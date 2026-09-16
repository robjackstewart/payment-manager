using PaymentManager.Domain.Entities;

namespace PaymentManager.Application.Common;

internal static class EffectiveValueResolver
{
    /// <summary>
    /// Resolves the amount in effect on a given date: the most recent effective value
    /// whose EffectiveDate is on or before <paramref name="asOfDate"/>, or
    /// <paramref name="initialAmount"/> if none apply yet. <paramref name="sortedValues"/>
    /// must be sorted by EffectiveDate ascending.
    /// </summary>
    public static decimal Resolve(IReadOnlyList<EffectivePaymentValue> sortedValues, DateOnly asOfDate, decimal initialAmount)
    {
        EffectivePaymentValue? current = null;
        foreach (var v in sortedValues)
        {
            if (v.EffectiveDate <= asOfDate)
                current = v;
            else
                break;
        }
        return current?.Amount ?? initialAmount;
    }
}
