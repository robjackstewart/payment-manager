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
            Amount = 250.50m,
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
            Amount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = null
        };
        var payments = new[] { matchingPayment, nonMatchingPayment };
        var paymentsDbSet = payments.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
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
        result.Amount.ShouldBe(matchingPayment.Amount);
        result.Currency.ShouldBe(matchingPayment.Currency);
        result.Frequency.ShouldBe(matchingPayment.Frequency);
        result.StartDate.ShouldBe(matchingPayment.StartDate);
        result.EndDate.ShouldBe(matchingPayment.EndDate);
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
            Amount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31)
        };
        var payments = new[] { nonMatchingPayment };
        var paymentsDbSet = payments.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        var logger = new FakeLogger<GetPayment.Handler>();
        var request = new GetPayment(Guid.NewGuid());
        var handler = new GetPayment.Handler(context, logger);
        var handle = new Func<Task>(() => handler.Handle(request, cancellationToken));

        // Act & Assert
        await handle.ShouldThrowAsync<NotFoundException<Payment>>();
    }
}
