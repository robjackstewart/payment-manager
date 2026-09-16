namespace PaymentManager.Domain.Entities;

/// <summary>
/// One person's share of a payment. Splits are the only link between a payment and the
/// people it concerns, and a payment's splits always total 100%.
/// </summary>
public record PaymentSplit()
{
    public required Guid PaymentId { get; init; }
    public required Guid PersonId { get; init; }
    public required decimal Percentage { get; init; }
}
