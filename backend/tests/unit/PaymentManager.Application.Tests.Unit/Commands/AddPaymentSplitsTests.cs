using FakeItEasy;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;
using static PaymentManager.Application.Common.Exceptions;
using DomainValidationException = PaymentManager.Application.Common.Exceptions.ValidationException;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class AddPaymentSplitsTests
{
    private static Payment MakePayment(
        PaymentDirection direction = PaymentDirection.Outgoing,
        Guid? payerGroupId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? userId = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            Direction = direction,
            StartDate = startDate ?? new DateOnly(2025, 1, 1),
            EndDate = endDate,
            PayerGroupId = payerGroupId
        };

    private static Person MakePerson(Guid userId) =>
        new() { Id = Guid.NewGuid(), UserId = userId, Name = Guid.NewGuid().ToString() };

    private static (IPaymentManagerContext context, DbSet<EffectivePaymentSplit> effectiveSplits) CreateContext(
        Payment? payment,
        EffectivePaymentSplit[] existing,
        Person[]? people = null,
        PayerGroup[]? groups = null,
        PayerGroupMember[]? members = null)
    {
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(payment);
        var effectiveSplitsDbSet = existing.BuildMockDbSet();
        A.CallTo(() => context.EffectivePaymentSplits).Returns(effectiveSplitsDbSet);
        A.CallTo(() => context.People).Returns((people ?? []).BuildMockDbSet());
        A.CallTo(() => context.PayerGroups).Returns((groups ?? []).BuildMockDbSet());
        A.CallTo(() => context.PayerGroupMembers).Returns((members ?? []).BuildMockDbSet());
        return (context, effectiveSplitsDbSet);
    }

    // ── Validator ─────────────────────────────────────────────────────────────

    [Test]
    public void Validator_Should_HaveError_When_PaymentIdIsEmpty()
    {
        var validator = new AddPaymentSplits.Validator();
        var result = validator.TestValidate(new AddPaymentSplits(Guid.Empty, new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(Guid.NewGuid(), 100m)]));
        result.ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Test]
    public void Validator_Should_HaveError_When_EffectiveDateIsDefault()
    {
        var validator = new AddPaymentSplits.Validator();
        var result = validator.TestValidate(new AddPaymentSplits(Guid.NewGuid(), default, [new AddPaymentSplits.SplitRequest(Guid.NewGuid(), 100m)]));
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Test]
    public void Validator_Should_HaveError_When_SplitsAreEmpty()
    {
        var validator = new AddPaymentSplits.Validator();
        var result = validator.TestValidate(new AddPaymentSplits(Guid.NewGuid(), new DateOnly(2025, 6, 1), []));
        result.ShouldHaveValidationErrorFor(x => x.Splits);
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(101)]
    public void Validator_Should_HaveError_When_PercentageOutOfRange(decimal percentage)
    {
        var validator = new AddPaymentSplits.Validator();
        var result = validator.TestValidate(new AddPaymentSplits(Guid.NewGuid(), new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(Guid.NewGuid(), percentage)]));
        result.ShouldHaveValidationErrorFor("Splits[0].Percentage");
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handler_Should_InsertSet_When_DateDoesNotExist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();
        var alice = MakePerson(payment.UserId);
        var bob = MakePerson(payment.UserId);
        var (context, effectiveSplits) = CreateContext(payment, [], [alice, bob]);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());
        var effectiveDate = new DateOnly(2025, 6, 1);

        var response = await handler.Handle(
            new AddPaymentSplits(payment.Id, effectiveDate, [new AddPaymentSplits.SplitRequest(alice.Id, 40m), new AddPaymentSplits.SplitRequest(bob.Id, 60m)]), ct);

        A.CallTo(() => effectiveSplits.Add(A<EffectivePaymentSplit>.That.Matches(s =>
            s.PaymentId == payment.Id && s.EffectiveDate == effectiveDate && s.PersonId == alice.Id && s.Percentage == 40m))).MustHaveHappenedOnceExactly();
        A.CallTo(() => effectiveSplits.Add(A<EffectivePaymentSplit>.That.Matches(s =>
            s.PaymentId == payment.Id && s.EffectiveDate == effectiveDate && s.PersonId == bob.Id && s.Percentage == 60m))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(ct)).MustHaveHappenedOnceExactly();
        response.PaymentId.ShouldBe(payment.Id);
        response.EffectiveDate.ShouldBe(effectiveDate);
        response.Splits.Count.ShouldBe(2);
    }

    [Test]
    public async Task Handler_Should_ReplaceExistingSet_When_DateAlreadyExists()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();
        var alice = MakePerson(payment.UserId);
        var bob = MakePerson(payment.UserId);
        var effectiveDate = new DateOnly(2025, 6, 1);
        var existing = new[]
        {
            new EffectivePaymentSplit { PaymentId = payment.Id, EffectiveDate = effectiveDate, PersonId = alice.Id, Percentage = 100m }
        };
        var (context, effectiveSplits) = CreateContext(payment, existing, [alice, bob]);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await handler.Handle(new AddPaymentSplits(payment.Id, effectiveDate, [new AddPaymentSplits.SplitRequest(alice.Id, 50m), new AddPaymentSplits.SplitRequest(bob.Id, 50m)]), ct);

        A.CallTo(() => effectiveSplits.RemoveRange(A<IEnumerable<EffectivePaymentSplit>>.That.Matches(rows => rows.Any(r => r.PersonId == alice.Id))))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => effectiveSplits.Add(A<EffectivePaymentSplit>._)).MustHaveHappened(2, Times.Exactly);
    }

    [Test]
    public async Task Handler_Should_ThrowNotFoundException_When_PaymentDoesNotExist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (context, _) = CreateContext(null, []);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await Should.ThrowAsync<NotFoundException<Payment>>(
            () => handler.Handle(new AddPaymentSplits(Guid.NewGuid(), new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(Guid.NewGuid(), 100m)]), ct));
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_PaymentIsIncoming()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment(PaymentDirection.Incoming);
        var person = MakePerson(payment.UserId);
        var (context, _) = CreateContext(payment, [], [person]);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        var exception = await Should.ThrowAsync<DomainValidationException>(
            () => handler.Handle(new AddPaymentSplits(payment.Id, new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(person.Id, 100m)]), ct));
        exception.Errors.ShouldContain(e => e.PropertyName == "EffectiveDate");
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_EffectiveDateNotAfterStartDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment(startDate: new DateOnly(2025, 3, 1));
        var person = MakePerson(payment.UserId);
        var (context, _) = CreateContext(payment, [], [person]);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await Should.ThrowAsync<DomainValidationException>(
            () => handler.Handle(new AddPaymentSplits(payment.Id, new DateOnly(2025, 3, 1), [new AddPaymentSplits.SplitRequest(person.Id, 100m)]), ct));
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_EffectiveDateAfterEndDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment(startDate: new DateOnly(2025, 1, 1), endDate: new DateOnly(2025, 6, 30));
        var person = MakePerson(payment.UserId);
        var (context, _) = CreateContext(payment, [], [person]);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await Should.ThrowAsync<DomainValidationException>(
            () => handler.Handle(new AddPaymentSplits(payment.Id, new DateOnly(2025, 12, 1), [new AddPaymentSplits.SplitRequest(person.Id, 100m)]), ct));
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_SplitsDoNotTotal100()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();
        var person = MakePerson(payment.UserId);
        var (context, _) = CreateContext(payment, [], [person]);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await Should.ThrowAsync<DomainValidationException>(
            () => handler.Handle(new AddPaymentSplits(payment.Id, new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(person.Id, 40m)]), ct));
    }

    [Test]
    public async Task Handler_Should_ThrowNotFoundException_When_PersonNotOwnedByUser()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();
        var someoneElsesPerson = MakePerson(Guid.NewGuid());
        var (context, _) = CreateContext(payment, [], [someoneElsesPerson]);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await Should.ThrowAsync<NotFoundException<Person>>(
            () => handler.Handle(new AddPaymentSplits(payment.Id, new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(someoneElsesPerson.Id, 100m)]), ct));
    }

    [Test]
    public async Task Handler_Should_Accept_When_GroupedOutgoingSplitsAreCurrentMembers()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var groupId = Guid.NewGuid();
        var payment = MakePayment(payerGroupId: groupId);
        var alice = MakePerson(payment.UserId);
        var bob = MakePerson(payment.UserId);
        var group = new PayerGroup { Id = groupId, UserId = payment.UserId, Name = "The Flat" };
        var members = new[]
        {
            new PayerGroupMember { PayerGroupId = groupId, PersonId = alice.Id },
            new PayerGroupMember { PayerGroupId = groupId, PersonId = bob.Id },
        };
        var (context, effectiveSplits) = CreateContext(payment, [], [alice, bob], [group], members);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await handler.Handle(new AddPaymentSplits(payment.Id, new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(alice.Id, 50m), new AddPaymentSplits.SplitRequest(bob.Id, 50m)]), ct);

        A.CallTo(() => effectiveSplits.Add(A<EffectivePaymentSplit>._)).MustHaveHappened(2, Times.Exactly);
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_GroupedSplitIncludesNonMember()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var groupId = Guid.NewGuid();
        var payment = MakePayment(payerGroupId: groupId);
        var member = MakePerson(payment.UserId);
        var outsider = MakePerson(payment.UserId);
        var group = new PayerGroup { Id = groupId, UserId = payment.UserId, Name = "The Flat" };
        var members = new[] { new PayerGroupMember { PayerGroupId = groupId, PersonId = member.Id } };
        var (context, _) = CreateContext(payment, [], [member, outsider], [group], members);
        var handler = new AddPaymentSplits.Handler(context, new FakeLogger<AddPaymentSplits.Handler>());

        await Should.ThrowAsync<DomainValidationException>(
            () => handler.Handle(new AddPaymentSplits(payment.Id, new DateOnly(2025, 6, 1), [new AddPaymentSplits.SplitRequest(member.Id, 50m), new AddPaymentSplits.SplitRequest(outsider.Id, 50m)]), ct));
    }
}

