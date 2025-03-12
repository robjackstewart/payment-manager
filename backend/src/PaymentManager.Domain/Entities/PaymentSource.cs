using System;

namespace PaymentManager.Domain.Entities;

public sealed class PaymentSource
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

    public required Guid UserId { get; init; }
    public User? User { get; private set; }
    public ICollection<Payment> Payments { get; private set; } = [];
}
