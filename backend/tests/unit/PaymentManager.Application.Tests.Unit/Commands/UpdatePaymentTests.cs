using NUnit.Framework;
using PaymentManager.Application.Commands;
using FluentValidation.TestHelper;
using FakeItEasy;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using MockQueryable.FakeItEasy;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class UpdatePaymentTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForId_When_Empty()
    {
        // Arrange
        var request = new UpdatePayment(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_Empty()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForPaymentSourceId_When_Empty()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentSourceId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForPayeeId_When_Empty()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PayeeId);
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    public void Validator_Should_HaveValidationErrorForAmount_When_ZeroOrNegative(decimal amount)
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForFrequency_When_Invalid()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", (PaymentFrequency)999, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Frequency);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForStartDate_When_Default()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, default, new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForEndDate_When_BeforeStartDate()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 6, 1), new DateOnly(2025, 1, 1));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForEndDate_When_FrequencyIsOnceAndEndDateIsSet()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForCurrency_When_Empty()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_UpdatePaymentInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 6, 1)
        };
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = new[] { existingPayment }.BuildMockDbSet();
        var splitsDbSet = Array.Empty<PaymentSplit>().BuildMockDbSet();
        var contactsDbSet = Array.Empty<Contact>().BuildMockDbSet();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.Contacts).Returns(contactsDbSet);
        var logger = new FakeLogger<UpdatePayment.Handler>();
        var newUserId = Guid.NewGuid();
        var newPaymentSourceId = Guid.NewGuid();
        var newPayeeId = Guid.NewGuid();
        var request = new UpdatePayment(existingPayment.Id, newUserId, newPaymentSourceId, newPayeeId, 500m, "EUR", PaymentFrequency.Annually, new DateOnly(2025, 3, 1), new DateOnly(2026, 3, 1));
        var handler = new UpdatePayment.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => paymentsDbSet.Update(A<Payment>.That.Matches(p =>
            p.UserId == newUserId &&
            p.PaymentSourceId == newPaymentSourceId &&
            p.PayeeId == newPayeeId &&
            p.Amount == 500m &&
            p.Currency == "EUR" &&
            p.Frequency == PaymentFrequency.Annually
        ))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
        response.ShouldNotBeNull();
        response.Id.ShouldBe(existingPayment.Id);
        response.UserId.ShouldBe(newUserId);
        response.PaymentSourceId.ShouldBe(newPaymentSourceId);
        response.PayeeId.ShouldBe(newPayeeId);
        response.Amount.ShouldBe(500m);
        response.Currency.ShouldBe("EUR");
        response.Frequency.ShouldBe(PaymentFrequency.Annually);
        response.StartDate.ShouldBe(new DateOnly(2025, 3, 1));
        response.EndDate.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PaymentDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = Array.Empty<Payment>().BuildMockDbSet();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        var logger = new FakeLogger<UpdatePayment.Handler>();
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var handler = new UpdatePayment.Handler(context, logger);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<Payment>>(() => handler.Handle(request, cancellationToken));
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForDescription_When_Over500Chars()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), new string('a', 501));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public async Task Handler_Handle_Should_UpdateDescription()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            Description = "Old description"
        };
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = new[] { existingPayment }.BuildMockDbSet();
        var splitsDbSet = Array.Empty<PaymentSplit>().BuildMockDbSet();
        var contactsDbSet = Array.Empty<Contact>().BuildMockDbSet();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.Contacts).Returns(contactsDbSet);
        var logger = new FakeLogger<UpdatePayment.Handler>();
        var request = new UpdatePayment(existingPayment.Id, existingPayment.UserId, existingPayment.PaymentSourceId, existingPayment.PayeeId, 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), null, "Updated description");
        var handler = new UpdatePayment.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => paymentsDbSet.Update(A<Payment>.That.Matches(p => p.Description == "Updated description"))).MustHaveHappenedOnceExactly();
        response.Description.ShouldBe("Updated description");
    }
}
