using PaymentManager.Domain.Enums;

namespace PaymentManager.Domain.Entities;

public record Payment()
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required Guid PaymentSourceId { get; init; }
    public required Guid PayeeId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required PaymentFrequency Frequency { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}
