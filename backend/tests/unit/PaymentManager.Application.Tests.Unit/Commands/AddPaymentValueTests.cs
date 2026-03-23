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

internal sealed class AddPaymentValueTests
{
    // ── Validator ─────────────────────────────────────────────────────────────

    [Test]
    public void Validator_Should_HaveValidationError_When_PaymentIdIsEmpty()
    {
        var validator = new AddPaymentValue.Validator();
        var result = validator.TestValidate(new AddPaymentValue(Guid.Empty, new DateOnly(2025, 1, 1), 50m));
        result.ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Test]
    public void Validator_Should_HaveValidationError_When_EffectiveDateIsDefault()
    {
        var validator = new AddPaymentValue.Validator();
        var result = validator.TestValidate(new AddPaymentValue(Guid.NewGuid(), default, 50m));
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    public void Validator_Should_HaveValidationError_When_AmountIsZeroOrNegative(decimal amount)
    {
        var validator = new AddPaymentValue.Validator();
        var result = validator.TestValidate(new AddPaymentValue(Guid.NewGuid(), new DateOnly(2025, 1, 1), amount));
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        var validator = new AddPaymentValue.Validator();
        var result = validator.TestValidate(new AddPaymentValue(Guid.NewGuid(), new DateOnly(2025, 6, 1), 99.99m));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    private static (IPaymentManagerContext context, DbSet<Payment> payments, DbSet<EffectivePaymentValue> values) CreateFakeContext(
        Payment payment, IReadOnlyList<EffectivePaymentValue> existingValues)
    {
        var context = A.Fake<IPaymentManagerContext>();

        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(payment);

        var valuesDbSet = existingValues.ToArray().BuildMockDbSet();
        A.CallTo(() => context.EffectivePaymentValues).Returns(valuesDbSet);

        return (context, paymentsDbSet, valuesDbSet);
    }

    [Test]
    public async Task Handler_Should_InsertNewValue_When_DateDoesNotExist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1)
        };
        var (context, _, valuesDbSet) = CreateFakeContext(payment, []);
        var logger = new FakeLogger<AddPaymentValue.Handler>();
        var handler = new AddPaymentValue.Handler(context, logger);
        var request = new AddPaymentValue(payment.Id, new DateOnly(2025, 6, 1), 75m);

        var response = await handler.Handle(request, ct);

        A.CallTo(() => valuesDbSet.Add(A<EffectivePaymentValue>.That.Matches(v =>
            v.PaymentId == payment.Id &&
            v.EffectiveDate == new DateOnly(2025, 6, 1) &&
            v.Amount == 75m))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(ct)).MustHaveHappenedOnceExactly();
        response.PaymentId.ShouldBe(payment.Id);
        response.EffectiveDate.ShouldBe(new DateOnly(2025, 6, 1));
        response.Amount.ShouldBe(75m);
    }

    [Test]
    public async Task Handler_Should_UpsertValue_When_DateAlreadyExists()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1)
        };
        var existingValue = new EffectivePaymentValue
        {
            PaymentId = payment.Id,
            EffectiveDate = new DateOnly(2025, 6, 1),
            Amount = 50m
        };
        var (context, _, valuesDbSet) = CreateFakeContext(payment, [existingValue]);
        var logger = new FakeLogger<AddPaymentValue.Handler>();
        var handler = new AddPaymentValue.Handler(context, logger);
        var request = new AddPaymentValue(payment.Id, new DateOnly(2025, 6, 1), 60m);

        var response = await handler.Handle(request, ct);

        A.CallTo(() => valuesDbSet.Remove(existingValue)).MustHaveHappenedOnceExactly();
        A.CallTo(() => valuesDbSet.Add(A<EffectivePaymentValue>.That.Matches(v =>
            v.PaymentId == payment.Id &&
            v.EffectiveDate == new DateOnly(2025, 6, 1) &&
            v.Amount == 60m))).MustHaveHappenedOnceExactly();
        response.Amount.ShouldBe(60m);
    }

    [Test]
    public async Task Handler_Should_ThrowNotFoundException_When_PaymentDoesNotExist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns((Payment?)null);
        var logger = new FakeLogger<AddPaymentValue.Handler>();
        var handler = new AddPaymentValue.Handler(context, logger);

        await Should.ThrowAsync<NotFoundException<Payment>>(
            () => handler.Handle(new AddPaymentValue(Guid.NewGuid(), new DateOnly(2025, 6, 1), 50m), ct));
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_EffectiveDateBeforeStartDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 3, 1)
        };
        var (context, _, _) = CreateFakeContext(payment, []);
        var logger = new FakeLogger<AddPaymentValue.Handler>();
        var handler = new AddPaymentValue.Handler(context, logger);

        await Should.ThrowAsync<Exceptions.ValidationException>(
            () => handler.Handle(new AddPaymentValue(payment.Id, new DateOnly(2025, 1, 1), 50m), ct));
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_EffectiveDateAfterEndDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 6, 30)
        };
        var (context, _, _) = CreateFakeContext(payment, []);
        var logger = new FakeLogger<AddPaymentValue.Handler>();
        var handler = new AddPaymentValue.Handler(context, logger);

        await Should.ThrowAsync<Exceptions.ValidationException>(
            () => handler.Handle(new AddPaymentValue(payment.Id, new DateOnly(2025, 12, 1), 50m), ct));
    }

    [Test]
    public async Task Handler_Should_ThrowValidationException_When_EffectiveDateEqualsStartDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 3, 1)
        };
        var (context, _, _) = CreateFakeContext(payment, []);
        var logger = new FakeLogger<AddPaymentValue.Handler>();
        var handler = new AddPaymentValue.Handler(context, logger);

        await Should.ThrowAsync<Exceptions.ValidationException>(
            () => handler.Handle(new AddPaymentValue(payment.Id, new DateOnly(2025, 3, 1), 50m), ct));
    }

    [Test]
    public async Task Handler_Should_Accept_When_PaymentHasNoEndDate_AndEffectiveDateIsFarFuture()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = null   // no upper bound
        };
        var (context, _, valuesDbSet) = CreateFakeContext(payment, []);
        var handler = new AddPaymentValue.Handler(context, new FakeLogger<AddPaymentValue.Handler>());
        var farFutureDate = new DateOnly(2099, 12, 31);

        var response = await handler.Handle(new AddPaymentValue(payment.Id, farFutureDate, 99m), ct);

        response.EffectiveDate.ShouldBe(farFutureDate);
        response.Amount.ShouldBe(99m);
        A.CallTo(() => valuesDbSet.Add(A<EffectivePaymentValue>.That.Matches(v =>
            v.EffectiveDate == farFutureDate && v.Amount == 99m))).MustHaveHappenedOnceExactly();
    }
}
