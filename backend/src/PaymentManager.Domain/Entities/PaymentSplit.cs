namespace PaymentManager.Domain.Entities;

public record PaymentSplit()
{
    public required Guid PaymentId { get; init; }
    public required Guid ContactId { get; init; }
    public required decimal Percentage { get; init; }
}
