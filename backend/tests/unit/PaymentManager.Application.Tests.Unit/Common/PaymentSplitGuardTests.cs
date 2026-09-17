using FakeItEasy;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Common;

internal sealed class PaymentSplitGuardTests
{
    private static IPaymentManagerContext CreateContext(
        Guid userId,
        Person[] people,
        PayerGroup[]? groups = null,
        PayerGroupMember[]? members = null)
    {
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(people.BuildMockDbSet());
        A.CallTo(() => context.PayerGroups).Returns((groups ?? []).BuildMockDbSet());
        A.CallTo(() => context.PayerGroupMembers).Returns((members ?? []).BuildMockDbSet());
        return context;
    }

    private static Person MakePerson(Guid userId, string name) =>
        new() { Id = Guid.NewGuid(), UserId = userId, Name = name };

    // ── Shared rules ──────────────────────────────────────────────────────────

    [Test]
    public async Task ValidateAsync_Should_ThrowValidationException_When_NoSplits()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(userId, []);

        var exception = await Should.ThrowAsync<ValidationException>(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Outgoing, null, null, TestContext.CurrentContext.CancellationToken));

        exception.Errors.ShouldContain(e => e.PropertyName == "Splits");
    }

    [Test]
    public async Task ValidateAsync_Should_ThrowValidationException_When_A_Person_Appears_Twice()
    {
        var userId = Guid.NewGuid();
        var person = MakePerson(userId, "Current User");
        var context = CreateContext(userId, [person]);

        var exception = await Should.ThrowAsync<ValidationException>(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Outgoing, null,
            [(person.Id, 50m), (person.Id, 50m)], TestContext.CurrentContext.CancellationToken));

        exception.Errors.ShouldContain(e => e.PropertyName == "Splits");
    }

    [Test]
    public async Task ValidateAsync_Should_ThrowValidationException_When_Splits_Do_Not_Total_100()
    {
        var userId = Guid.NewGuid();
        var person = MakePerson(userId, "Current User");
        var context = CreateContext(userId, [person]);

        var exception = await Should.ThrowAsync<ValidationException>(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Outgoing, null,
            [(person.Id, 40m)], TestContext.CurrentContext.CancellationToken));

        exception.Errors.ShouldContain(e => e.PropertyName == "Splits");
    }

    [Test]
    public async Task ValidateAsync_Should_ThrowNotFoundException_When_Person_Is_Unknown()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(userId, []);

        await Should.ThrowAsync<NotFoundException<Person>>(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Outgoing, null,
            [(Guid.NewGuid(), 100m)], TestContext.CurrentContext.CancellationToken));
    }

    // ── Income ────────────────────────────────────────────────────────────────

    [Test]
    public async Task ValidateAsync_Should_NotThrow_When_Income_Has_One_Person_At_100()
    {
        var userId = Guid.NewGuid();
        var person = MakePerson(userId, "Current User");
        var context = CreateContext(userId, [person]);

        await Should.NotThrowAsync(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Incoming, null,
            [(person.Id, 100m)], TestContext.CurrentContext.CancellationToken));
    }

    [Test]
    public async Task ValidateAsync_Should_ThrowValidationException_When_Income_Has_Multiple_People()
    {
        var userId = Guid.NewGuid();
        var personA = MakePerson(userId, "Current User");
        var personB = MakePerson(userId, "Jane Doe");
        var context = CreateContext(userId, [personA, personB]);

        var exception = await Should.ThrowAsync<ValidationException>(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Incoming, null,
            [(personA.Id, 50m), (personB.Id, 50m)], TestContext.CurrentContext.CancellationToken));

        exception.Errors.ShouldContain(e => e.PropertyName == "Splits");
    }

    [Test]
    public async Task ValidateAsync_Should_ThrowValidationException_When_Income_Has_PayerGroup()
    {
        var userId = Guid.NewGuid();
        var person = MakePerson(userId, "Current User");
        var group = new PayerGroup { Id = Guid.NewGuid(), UserId = userId, Name = "The Flat" };
        var context = CreateContext(
            userId,
            [person],
            [group],
            [new PayerGroupMember { PayerGroupId = group.Id, PersonId = person.Id }]);

        var exception = await Should.ThrowAsync<ValidationException>(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Incoming, group.Id,
            [(person.Id, 100m)], TestContext.CurrentContext.CancellationToken));

        exception.Errors.ShouldContain(e => e.PropertyName == "PayerGroupId");
    }

    // ── Outgoing ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ValidateAsync_Should_NotThrow_When_Ungrouped_Outgoing_Splits_Any_People()
    {
        var userId = Guid.NewGuid();
        var personA = MakePerson(userId, "Current User");
        var personB = MakePerson(userId, "Jane Doe");
        var context = CreateContext(userId, [personA, personB]);

        await Should.NotThrowAsync(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Outgoing, null,
            [(personA.Id, 50m), (personB.Id, 50m)], TestContext.CurrentContext.CancellationToken));
    }

    [Test]
    public async Task ValidateAsync_Should_NotThrow_When_Grouped_Outgoing_Splits_Are_Members()
    {
        var userId = Guid.NewGuid();
        var memberA = MakePerson(userId, "Current User");
        var memberB = MakePerson(userId, "Jane Doe");
        var group = new PayerGroup { Id = Guid.NewGuid(), UserId = userId, Name = "The Flat" };
        var context = CreateContext(
            userId,
            [memberA, memberB],
            [group],
            [
                new PayerGroupMember { PayerGroupId = group.Id, PersonId = memberA.Id },
                new PayerGroupMember { PayerGroupId = group.Id, PersonId = memberB.Id }
            ]);

        await Should.NotThrowAsync(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Outgoing, group.Id,
            [(memberA.Id, 50m), (memberB.Id, 50m)], TestContext.CurrentContext.CancellationToken));
    }

    [Test]
    public async Task ValidateAsync_Should_ThrowValidationException_When_Grouped_Outgoing_Splits_A_NonMember()
    {
        var userId = Guid.NewGuid();
        var member = MakePerson(userId, "Current User");
        var outsider = MakePerson(userId, "Derek");
        var group = new PayerGroup { Id = Guid.NewGuid(), UserId = userId, Name = "The Flat" };
        var context = CreateContext(
            userId,
            [member, outsider],
            [group],
            [new PayerGroupMember { PayerGroupId = group.Id, PersonId = member.Id }]);

        var exception = await Should.ThrowAsync<ValidationException>(() => PaymentSplitGuard.ValidateAsync(
            context, userId, PaymentDirection.Outgoing, group.Id,
            [(member.Id, 50m), (outsider.Id, 50m)], TestContext.CurrentContext.CancellationToken));

        exception.Errors.ShouldContain(e => e.PropertyName == "Splits");
    }
}
