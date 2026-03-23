using FakeItEasy;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class RemovePaymentValueTests
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
        StartDate = new DateOnly(2025, 1, 1)
    };

    // ── Validator ─────────────────────────────────────────────────────────────

    [Test]
    public void Validator_Should_HaveError_When_PaymentIdIsEmpty()
    {
        var validator = new RemovePaymentValue.Validator();
        var result = validator.TestValidate(new RemovePaymentValue(Guid.Empty, new DateOnly(2025, 6, 1)));
        result.ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Test]
    public void Validator_Should_HaveError_When_EffectiveDateIsDefault()
    {
        var validator = new RemovePaymentValue.Validator();
        var result = validator.TestValidate(new RemovePaymentValue(Guid.NewGuid(), default));
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Test]
    public void Validator_Should_NotHaveErrors_When_Valid()
    {
        var validator = new RemovePaymentValue.Validator();
        var result = validator.TestValidate(new RemovePaymentValue(Guid.NewGuid(), new DateOnly(2025, 6, 1)));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handler_Should_RemoveValue_When_Found()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();
        var effectiveDate = new DateOnly(2025, 6, 1);
        var value = new EffectivePaymentValue { PaymentId = payment.Id, EffectiveDate = effectiveDate, Amount = 75m };

        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(payment);
        var valuesDbSet = new[] { value }.BuildMockDbSet();
        A.CallTo(() => context.EffectivePaymentValues).Returns(valuesDbSet);

        var handler = new RemovePaymentValue.Handler(context, new FakeLogger<RemovePaymentValue.Handler>());
        await handler.Handle(new RemovePaymentValue(payment.Id, effectiveDate), ct);

        A.CallTo(() => valuesDbSet.Remove(value)).MustHaveHappenedOnceExactly();
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

        var handler = new RemovePaymentValue.Handler(context, new FakeLogger<RemovePaymentValue.Handler>());
        await Should.ThrowAsync<NotFoundException<Payment>>(
            () => handler.Handle(new RemovePaymentValue(Guid.NewGuid(), new DateOnly(2025, 6, 1)), ct));
    }

    [Test]
    public async Task Handler_Should_Throw_When_ValueNotFound()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = MakePayment();

        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(payment);
        var valuesDbSet = Array.Empty<EffectivePaymentValue>().BuildMockDbSet();
        A.CallTo(() => context.EffectivePaymentValues).Returns(valuesDbSet);

        var handler = new RemovePaymentValue.Handler(context, new FakeLogger<RemovePaymentValue.Handler>());
        await Should.ThrowAsync<NotFoundException<EffectivePaymentValue>>(
            () => handler.Handle(new RemovePaymentValue(payment.Id, new DateOnly(2025, 6, 1)), ct));
    }
}
