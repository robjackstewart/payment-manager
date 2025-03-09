namespace PaymentManager.Domain.Entities;

public record User()
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
