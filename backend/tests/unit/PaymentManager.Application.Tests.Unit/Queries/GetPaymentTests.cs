using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetPaymentTests
{
    [Test]
    public async Task Handler_Handle_Should_Return_PaymentWithMatchingId_When_PaymentWithMatchingIdExistsInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var matchingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 250.50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31)
        };
        var nonMatchingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 99m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = null
        };
        var payments = new[] { matchingPayment, nonMatchingPayment };
        var paymentsDbSet = payments.BuildMockDbSet();
        var splitsDbSet = Array.Empty<PaymentSplit>().BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.EffectivePaymentValues).Returns(new[]
        {
            new EffectivePaymentValue { PaymentId = matchingPayment.Id, EffectiveDate = new DateOnly(2025, 1, 1), Amount = 250.50m }
        }.BuildMockDbSet());
        var logger = new FakeLogger<GetPayment.Handler>();
        var request = new GetPayment(matchingPayment.Id);
        var handler = new GetPayment.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(matchingPayment.Id);
        result.UserId.ShouldBe(matchingPayment.UserId);
        result.PaymentSourceId.ShouldBe(matchingPayment.PaymentSourceId);
        result.PayeeId.ShouldBe(matchingPayment.PayeeId);
        result.CurrentAmount.ShouldBe(250.50m);
        result.Currency.ShouldBe(matchingPayment.Currency);
        result.Frequency.ShouldBe(matchingPayment.Frequency);
        result.StartDate.ShouldBe(matchingPayment.StartDate);
        result.EndDate.ShouldBe(matchingPayment.EndDate);
        result.UserShare.Percentage.ShouldBe(100m);   // no splits → user owns 100%
        result.UserShare.Value.ShouldBe(250.50m);
        result.Splits.ShouldBeEmpty();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PaymentWithMatchingIdDoesNotExistInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var nonMatchingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 99m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31)
        };
        var payments = new[] { nonMatchingPayment };
        var paymentsDbSet = payments.BuildMockDbSet();
        var splitsDbSet = Array.Empty<PaymentSplit>().BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.EffectivePaymentValues).Returns(new[]
        {
            new EffectivePaymentValue { PaymentId = nonMatchingPayment.Id, EffectiveDate = new DateOnly(2025, 1, 1), Amount = 100m }
        }.BuildMockDbSet());
        var logger = new FakeLogger<GetPayment.Handler>();
        var request = new GetPayment(Guid.NewGuid());
        var handler = new GetPayment.Handler(context, logger);
        var handle = new Func<Task>(() => handler.Handle(request, cancellationToken));

        // Act & Assert
        await handle.ShouldThrowAsync<NotFoundException<Payment>>();
    }

    [Test]
    public async Task Handler_Handle_Should_Use_InitialAmount_When_NoEffectivePaymentValuesExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 75m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
        };
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(new[] { payment }.BuildMockDbSet());
        A.CallTo(() => context.PaymentSplits).Returns(Array.Empty<PaymentSplit>().BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentValues).Returns(Array.Empty<EffectivePaymentValue>().BuildMockDbSet());
        var handler = new GetPayment.Handler(context, new FakeLogger<GetPayment.Handler>());

        // Act
        var result = await handler.Handle(new GetPayment(payment.Id), cancellationToken);

        // Assert
        result.CurrentAmount.ShouldBe(75m);
        result.Values.ShouldBeEmpty();
        result.UserShare.Percentage.ShouldBe(100m);
        result.UserShare.Value.ShouldBe(75m);
    }

    [Test]
    public async Task Handler_Handle_Should_Compute_UserShareAndSplitValues_For_PaymentWithSplits()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var contactId = Guid.NewGuid();
        var payment = new Payment
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
        var splits = new[] { new PaymentSplit { PaymentId = payment.Id, ContactId = contactId, Percentage = 40m } };
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(new[] { payment }.BuildMockDbSet());
        A.CallTo(() => context.PaymentSplits).Returns(splits.BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentValues).Returns(new[]
        {
            new EffectivePaymentValue { PaymentId = payment.Id, EffectiveDate = new DateOnly(2025, 1, 1), Amount = 100m }
        }.BuildMockDbSet());
        var handler = new GetPayment.Handler(context, new FakeLogger<GetPayment.Handler>());

        // Act
        var result = await handler.Handle(new GetPayment(payment.Id), cancellationToken);

        // Assert
        result.UserShare.Percentage.ShouldBe(60m);    // 100 - 40
        result.UserShare.Value.ShouldBe(60m);          // 100 * 60 / 100
        result.Splits.Single().Percentage.ShouldBe(40m);
        result.Splits.Single().Value.ShouldBe(40m);   // 100 * 40 / 100
    }
}
