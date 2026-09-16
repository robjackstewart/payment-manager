using Microsoft.EntityFrameworkCore;
using PaymentManager.Application.Common.Validation;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Common.Exceptions;
using DomainValidationException = PaymentManager.Application.Common.Exceptions.ValidationException;

namespace PaymentManager.Application.Common;

/// <summary>
/// Enforces the invariants that make splits the single link between a payment and the people
/// it concerns:
/// <list type="bullet">
/// <item>every payment has at least one split, and its splits total exactly 100%;</item>
/// <item>every split person belongs to the user;</item>
/// <item>incoming payments belong to people, not groups, so they carry no payer group;</item>
/// <item>an outgoing payment in a group may only be split across that group's members;</item>
/// <item>an outgoing payment with no group may be split across any of the user's people.</item>
/// </list>
/// Keeping these server-side is what lets the dashboard roll every split up under exactly one
/// group, and lets a person's commitments be summed without special-casing the user.
/// </summary>
internal static class PaymentSplitGuard
{
    public static async Task ValidateAsync(
        IPaymentManagerContext context,
        Guid userId,
        PaymentDirection direction,
        Guid? payerGroupId,
        IReadOnlyList<(Guid PersonId, decimal Percentage)>? splits,
        CancellationToken cancellationToken)
    {
        if (splits is not { Count: > 0 })
        {
            throw Invalid("Splits", "A payment must be split across at least one person.");
        }

        var duplicatePersonIds = splits
            .GroupBy(s => s.PersonId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();
        if (duplicatePersonIds.Length > 0)
        {
            throw Invalid("Splits", $"A person can only appear once per payment: {string.Join(", ", duplicatePersonIds)}");
        }

        var total = splits.Sum(s => s.Percentage);
        if (total != 100m)
        {
            throw Invalid("Splits", $"Split percentages must total exactly 100 (got {total}).");
        }

        var requestedPersonIds = splits.Select(s => s.PersonId).ToHashSet();
        var people = await context.People
            .Where(p => requestedPersonIds.Contains(p.Id) && p.UserId == userId)
            .Select(p => p.Id)
            .ToArrayAsync(cancellationToken);

        var missingIds = requestedPersonIds.Except(people).ToArray();
        if (missingIds.Length > 0)
        {
            throw new NotFoundException<Person>($"PersonIds not found: {string.Join(", ", missingIds)}");
        }

        if (direction == PaymentDirection.Incoming)
        {
            if (payerGroupId.HasValue)
            {
                throw Invalid(
                    "PayerGroupId",
                    "Income belongs to a person, not a payer group. A group's income is the total of its members' income.");
            }

            return;
        }

        // An ungrouped outgoing payment may be split across any of the user's people.
        if (!payerGroupId.HasValue)
        {
            return;
        }

        var groupExists = await context.PayerGroups
            .AnyAsync(g => g.Id == payerGroupId.Value && g.UserId == userId, cancellationToken);
        if (!groupExists)
        {
            throw new NotFoundException<PayerGroup>($"Id: {payerGroupId.Value}");
        }

        var memberIds = await context.PayerGroupMembers
            .Where(m => m.PayerGroupId == payerGroupId.Value)
            .Select(m => m.PersonId)
            .ToHashSetAsync(cancellationToken);

        var nonMembers = requestedPersonIds.Except(memberIds).ToArray();
        if (nonMembers.Length > 0)
        {
            throw Invalid("Splits", $"People are not members of the payer group: {string.Join(", ", nonMembers)}");
        }
    }

    private static DomainValidationException Invalid(string propertyName, string error) =>
        new([new ValidationError { PropertyName = propertyName, Errors = [error] }]);
}
