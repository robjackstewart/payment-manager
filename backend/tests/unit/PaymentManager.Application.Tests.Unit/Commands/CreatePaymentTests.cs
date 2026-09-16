using NUnit.Framework;
using PaymentManager.Application.Commands;
using FluentValidation.TestHelper;
using FakeItEasy;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using MockQueryable.FakeItEasy;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;
using PaymentManager.Application.Common;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class CreatePaymentTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_Empty()
    {
        // Arrange
        var request = new CreatePayment(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForPaymentSourceId_When_Empty()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentSourceId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForPayeeId_When_Empty()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PayeeId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForAmount_When_ZeroOrNegative_Zero()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    public void Validator_Should_HaveValidationErrorForAmount_When_ZeroOrNegative(decimal amount)
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForFrequency_When_Invalid()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", (PaymentFrequency)999, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Frequency);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForStartDate_When_Default()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, default, new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForEndDate_When_BeforeStartDate()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 6, 1), new DateOnly(2025, 1, 1));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForEndDate_When_FrequencyIsOnceAndEndDateIsSet()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Once, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForCurrency_When_Empty()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_OncePaymentWithNullEndDate()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Once, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), null);
        var validator = new CreatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_AddPaymentToContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var payments = Array.Empty<Payment>();
        var paymentsDbSet = payments.BuildMockDbSet();
        A.CallTo(() => paymentsDbSet.Add(A<Payment>._)).Invokes((Payment p) => payments = payments.Append(p).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.People).Returns(new[] { new Person { Id = ownerId, UserId = userId, Name = "Current User" } }.BuildMockDbSet());
        var logger = new FakeLogger<CreatePayment.Handler>();
        var startDate = new DateOnly(2025, 1, 1);
        var request = new CreatePayment(userId, Guid.NewGuid(), Guid.NewGuid(), 250.50m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, startDate, new DateOnly(2025, 12, 31),
            Splits: [new CreatePayment.SplitRequest(ownerId, 100m)]);
        var handler = new CreatePayment.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        payments.ShouldNotBeNull();
        payments.ShouldHaveSingleItem();
        payments.First().UserId.ShouldBe(request.UserId);
        payments.First().PaymentSourceId.ShouldBe(request.PaymentSourceId);
        payments.First().PayeeId.ShouldBe(request.PayeeId);
        payments.First().InitialAmount.ShouldBe(250.50m);
        payments.First().Currency.ShouldBe(request.Currency);
        payments.First().Frequency.ShouldBe(request.Frequency);
        payments.First().StartDate.ShouldBe(request.StartDate);
        payments.First().EndDate.ShouldBe(request.EndDate);
        response.ShouldNotBeNull();
        response.Id.ShouldNotBe(Guid.Empty);
        response.UserId.ShouldBe(request.UserId);
        response.PaymentSourceId.ShouldBe(request.PaymentSourceId);
        response.PayeeId.ShouldBe(request.PayeeId);
        response.CurrentAmount.ShouldBe(250.50m);
        response.Values.ShouldBeEmpty();
        response.Currency.ShouldBe(request.Currency);
        response.Frequency.ShouldBe(request.Frequency);
        response.StartDate.ShouldBe(request.StartDate);
        response.EndDate.ShouldBe(request.EndDate);
        response.Splits.Single().PersonId.ShouldBe(ownerId);
        response.Splits.Single().Value.ShouldBe(250.50m);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForDescription_When_Over500Chars()
    {
        // Arrange
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), new string('a', 501));
        var validator = new CreatePayment.Validator();
        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public async Task Handler_Handle_Should_StoreDescription()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var payments = Array.Empty<Payment>();
        var paymentsDbSet = payments.BuildMockDbSet();
        A.CallTo(() => paymentsDbSet.Add(A<Payment>._)).Invokes((Payment p) => payments = payments.Append(p).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.People).Returns(new[] { new Person { Id = ownerId, UserId = userId, Name = "Current User" } }.BuildMockDbSet());
        var logger = new FakeLogger<CreatePayment.Handler>();
        var request = new CreatePayment(userId, Guid.NewGuid(), Guid.NewGuid(), 9.99m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), null, "My monthly subscription",
            Splits: [new CreatePayment.SplitRequest(ownerId, 100m)]);
        var handler = new CreatePayment.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        payments.First().Description.ShouldBe("My monthly subscription");
        response.Description.ShouldBe("My monthly subscription");
    }

    [Test]
    public void Validator_Should_HaveValidationError_When_A_SplitPercentageExceeds100()
    {
        // Individual split bounds are checked by the validator; the "splits must sum to
        // exactly 100" rule lives in PaymentSplitGuard, at the handler level.
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), null, null, Guid.NewGuid(),
            [new CreatePayment.SplitRequest(Guid.NewGuid(), 110m)]);
        var result = new CreatePayment.Validator().TestValidate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void Validator_Should_HaveValidationError_When_SplitPercentageIsZero()
    {
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), null, null, Guid.NewGuid(),
            [new CreatePayment.SplitRequest(Guid.NewGuid(), 0m)]);
        var result = new CreatePayment.Validator().TestValidate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_SplitsAreValid()
    {
        var request = new CreatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), null, null, Guid.NewGuid(),
            [new CreatePayment.SplitRequest(Guid.NewGuid(), 30m), new CreatePayment.SplitRequest(Guid.NewGuid(), 20m)]);
        var result = new CreatePayment.Validator().TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_Compute_SplitValues_When_RequestHasSplits()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var payerGroup = new PayerGroup { Id = Guid.NewGuid(), UserId = userId, Name = "Family" };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(Array.Empty<Payment>().BuildMockDbSet());
        A.CallTo(() => context.PaymentSplits).Returns(Array.Empty<PaymentSplit>().BuildMockDbSet());
        A.CallTo(() => context.PayerGroups).Returns(new[] { payerGroup }.BuildMockDbSet());
        A.CallTo(() => context.People).Returns(new[]
        {
            new Person { Id = ownerId, UserId = userId, Name = "Current User" },
            new Person { Id = personId, UserId = userId, Name = "Alice" }
        }.BuildMockDbSet());
        A.CallTo(() => context.PayerGroupMembers).Returns(new[]
        {
            new PayerGroupMember { PayerGroupId = payerGroup.Id, PersonId = ownerId },
            new PayerGroupMember { PayerGroupId = payerGroup.Id, PersonId = personId }
        }.BuildMockDbSet());
        var logger = new FakeLogger<CreatePayment.Handler>();
        var request = new CreatePayment(
            userId, Guid.NewGuid(), Guid.NewGuid(), 200m, "USD",
            PaymentFrequency.Monthly, PaymentDirection.Outgoing, new DateOnly(2025, 1, 1), null, null, payerGroup.Id,
            [new CreatePayment.SplitRequest(personId, 25m), new CreatePayment.SplitRequest(ownerId, 75m)]);
        var handler = new CreatePayment.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        response.Splits.Count.ShouldBe(2);
        response.Splits.Single(s => s.PersonId == personId).Value.ShouldBe(50m);   // 200 * 25 / 100
        response.Splits.Single(s => s.PersonId == ownerId).Value.ShouldBe(150m);    // 200 * 75 / 100
    }
}
