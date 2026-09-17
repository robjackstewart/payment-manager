namespace PaymentManager.Domain.Entities;

/// <summary>
/// One person's share of a payment from a given date onward. Together with <see cref="PaymentSplit"/>
/// (the initial split, in effect from the payment's start date) these form the dated split sets that
/// <c>EffectiveSplitResolver</c> steps through over time.
/// </summary>
public record EffectivePaymentSplit()
{
    public required Guid PaymentId { get; init; }
    public required DateOnly EffectiveDate { get; init; }
    public required Guid PersonId { get; init; }
    public required decimal Percentage { get; init; }
}
