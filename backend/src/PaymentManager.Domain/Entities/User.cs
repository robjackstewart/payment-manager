namespace PaymentManager.Domain.Entities;

public record User()
{
    public Guid Id { get; private set; } = Guid.Empty;
    public required string Name { get; init; }
}
