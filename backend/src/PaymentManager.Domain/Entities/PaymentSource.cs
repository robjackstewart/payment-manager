using System;

namespace PaymentManager.Domain.Entities;

public sealed class PaymentSource
{
    public Guid Id { get; private set; } = Guid.Empty;
    public required string Name { get; init; }
    public required string? Description { get; init; }

    public required Guid UserId { get; init; }
    public User? User { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
}
