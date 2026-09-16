namespace PaymentManager.Domain.Entities;

/// <summary>
/// Anyone money can be apportioned to. The signed-in user is not special: they are simply one
/// more person, so a payment's participants are always just a list of people.
/// </summary>
public record Person()
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
}
