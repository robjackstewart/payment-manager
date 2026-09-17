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

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class RemovePaymentSplitsTests
{
    private static Payment MakePayment() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        PaymentSourceId = Guid.NewGuid(),
        PayeeId = Guid.NewGuid(),
        InitialAmount = 100m,
        Currency = "USD",
        Frequency = PaymentFrequency.Monthly,
        Direction = PaymentDirection.Outgoing,
        StartDate = new DateOnly(2025, 1, 1)
    };

    // ── Validator ─────────────────────────────────────────────────────────────

    [Test]
    public void Validator_Should_HaveError_When_PaymentIdIsEmpty()
    {
        var validator = new RemovePaymentSplits.Validator();
        var result = validator.TestValidate(new RemovePaymentSplits(Guid.Empty, new DateOnly(2025, 6, 1)));
        result.ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Test]
    public void Validator_Should_HaveError_When_EffectiveDateIsDefault()
    {
        var validator = new RemovePaymentSplits.Validator();
        var result = validator.TestValidate(new RemovePaymentSplits(Guid.NewGuid(), default));
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Test]
    public void Validator_Should_NotHaveErrors_When_Valid()
    {
        var validator = new RemovePaymentSplits.Validator();
        var result = validator.TestValidate(new RemovePaymentSplits(Guid.NewGuid(), new DateOnly(2025, 6, 1)));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handler_Should_RemoveWholeSet_When_Found()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();
        var effectiveDate = new DateOnly(2025, 6, 1);
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var splits = new[]
        {
            new EffectivePaymentSplit { PaymentId = payment.Id, EffectiveDate = effectiveDate, PersonId = alice, Percentage = 60m },
            new EffectivePaymentSplit { PaymentId = payment.Id, EffectiveDate = effectiveDate, PersonId = bob, Percentage = 40m },
        };

        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(payment);
        var splitsDbSet = splits.BuildMockDbSet();
        A.CallTo(() => context.EffectivePaymentSplits).Returns(splitsDbSet);

        var handler = new RemovePaymentSplits.Handler(context, new FakeLogger<RemovePaymentSplits.Handler>());
        await handler.Handle(new RemovePaymentSplits(payment.Id, effectiveDate), ct);

        A.CallTo(() => splitsDbSet.RemoveRange(A<IEnumerable<EffectivePaymentSplit>>.That.Matches(rows => rows.Count() == 2)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(ct)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Should_Throw_When_PaymentNotFound()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns((Payment?)null);

        var handler = new RemovePaymentSplits.Handler(context, new FakeLogger<RemovePaymentSplits.Handler>());
        await Should.ThrowAsync<NotFoundException<Payment>>(
            () => handler.Handle(new RemovePaymentSplits(Guid.NewGuid(), new DateOnly(2025, 6, 1)), ct));
    }

    [Test]
    public async Task Handler_Should_Throw_When_NoSetForDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();

        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(payment);
        A.CallTo(() => context.EffectivePaymentSplits).Returns(Array.Empty<EffectivePaymentSplit>().BuildMockDbSet());

        var handler = new RemovePaymentSplits.Handler(context, new FakeLogger<RemovePaymentSplits.Handler>());
        await Should.ThrowAsync<NotFoundException<EffectivePaymentSplit>>(
            () => handler.Handle(new RemovePaymentSplits(payment.Id, new DateOnly(2025, 6, 1)), ct));
    }
}
