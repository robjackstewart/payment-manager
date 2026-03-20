namespace PaymentManager.Domain.Entities;

public record PaymentSource()
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
}
