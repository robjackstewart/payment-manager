namespace PaymentManager.Domain.Entities;

public record EffectivePaymentValue
{
    public required Guid Id { get; init; }
    public required Guid PaymentId { get; init; }
    public required DateOnly EffectiveDate { get; init; }
    public required decimal Amount { get; init; }
}
