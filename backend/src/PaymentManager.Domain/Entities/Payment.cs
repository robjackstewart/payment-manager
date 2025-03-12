using System;

namespace PaymentManager.Domain.Entities;

public sealed class Payment
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required decimal Amount { get; init; }
    public required Guid UserId { get; init; }
    public required PaymentSchedule Schedule { get; init; }

    public User User { get; private set; }
}
