using NUnit.Framework;
using PaymentManager.Application.Commands;
using FakeItEasy;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class DeletePaymentTests
{
    [Test]
    public async Task Handler_Handle_Should_DeletePaymentFromContext()
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
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31)
        };
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(existingPayment);
        var logger = new FakeLogger<DeletePayment.Handler>();
        var request = new DeletePayment(existingPayment.Id);
        var handler = new DeletePayment.Handler(context, logger);

        // Act
        await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => context.Payments.Remove(A<Payment>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PaymentDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var paymentsDbSet = A.Fake<DbSet<Payment>>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => paymentsDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns((Payment?)null);
        var logger = new FakeLogger<DeletePayment.Handler>();
        var request = new DeletePayment(Guid.NewGuid());
        var handler = new DeletePayment.Handler(context, logger);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<Payment>>(() => handler.Handle(request, cancellationToken));
    }
}
