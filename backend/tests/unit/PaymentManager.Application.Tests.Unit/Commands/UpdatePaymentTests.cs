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
    public void Validator_Should_HaveValidationErrorForInitialAmount_When_ZeroOrNegative(decimal initialAmount)
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), initialAmount, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        var validator = new UpdatePayment.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InitialAmount);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForFrequency_When_Invalid()
    {
        // Arrange
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", (PaymentFrequency)999, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));        var validator = new UpdatePayment.Validator();

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
            InitialAmount = 300m,
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
        A.CallTo(() => context.EffectivePaymentValues).Returns(Array.Empty<EffectivePaymentValue>().BuildMockDbSet());
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
            p.InitialAmount == 500m &&
            p.Currency == "EUR" &&
            p.Frequency == PaymentFrequency.Annually
        ))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
        response.ShouldNotBeNull();
        response.Id.ShouldBe(existingPayment.Id);
        response.UserId.ShouldBe(newUserId);
        response.PaymentSourceId.ShouldBe(newPaymentSourceId);
        response.PayeeId.ShouldBe(newPayeeId);
        response.CurrentAmount.ShouldBe(500m);
        response.Currency.ShouldBe("EUR");
        response.Frequency.ShouldBe(PaymentFrequency.Annually);
        response.StartDate.ShouldBe(new DateOnly(2025, 3, 1));
        response.EndDate.ShouldBe(new DateOnly(2026, 3, 1));
        response.UserShare.Percentage.ShouldBe(100m);     // no splits → user owns 100%
        response.UserShare.Value.ShouldBe(500m);
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
        var request = new UpdatePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), null, new string('a', 501));
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
            InitialAmount = 100m,
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
        A.CallTo(() => context.EffectivePaymentValues).Returns(Array.Empty<EffectivePaymentValue>().BuildMockDbSet());
        var logger = new FakeLogger<UpdatePayment.Handler>();
        var request = new UpdatePayment(existingPayment.Id, existingPayment.UserId, existingPayment.PaymentSourceId, existingPayment.PayeeId, 100m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), null, "Updated description");
        var handler = new UpdatePayment.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => paymentsDbSet.Update(A<Payment>.That.Matches(p => p.Description == "Updated description"))).MustHaveHappenedOnceExactly();
        response.Description.ShouldBe("Updated description");
    }

    [Test]
    public async Task Handler_Handle_Should_Compute_UserShareAndSplitValues_When_RequestHasSplits()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var contactId = Guid.NewGuid();
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
        };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(new[] { existingPayment }.BuildMockDbSet());
        A.CallTo(() => context.PaymentSplits).Returns(Array.Empty<PaymentSplit>().BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentValues).Returns(Array.Empty<EffectivePaymentValue>().BuildMockDbSet());
        A.CallTo(() => context.Contacts).Returns(new[]
        {
            new Contact { Id = contactId, UserId = existingPayment.UserId, Name = "Alice" }
        }.BuildMockDbSet());
        var logger = new FakeLogger<UpdatePayment.Handler>();
        var request = new UpdatePayment(
            existingPayment.Id, existingPayment.UserId, existingPayment.PaymentSourceId, existingPayment.PayeeId,
            400m, "USD", PaymentFrequency.Monthly, new DateOnly(2025, 1, 1), null, null,
            [new UpdatePayment.SplitRequest(contactId, 50m)]);
        var handler = new UpdatePayment.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        response.UserShare.Percentage.ShouldBe(50m);       // 100 - 50
        response.UserShare.Value.ShouldBe(200m);            // 400m * 50 / 100
        response.Splits.Single().Percentage.ShouldBe(50m);
        response.Splits.Single().Value.ShouldBe(200m);     // 400m * 50 / 100
    }
}
