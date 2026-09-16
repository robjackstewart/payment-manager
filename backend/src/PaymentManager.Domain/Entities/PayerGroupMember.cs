namespace PaymentManager.Domain.Entities;

/// <summary>Membership of a <see cref="Person"/> in a <see cref="PayerGroup"/>. A person may
/// belong to any number of groups.</summary>
public record PayerGroupMember()
{
    public required Guid PayerGroupId { get; init; }
    public required Guid PersonId { get; init; }
}
