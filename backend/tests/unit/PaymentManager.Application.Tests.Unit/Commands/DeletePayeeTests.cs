using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using Shouldly;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class DeletePayeeTests
{
    [Test]
    public async Task Handler_Handle_Should_RemovePayeeFromContext_When_PayeeExists()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var payeeId = Guid.NewGuid();
        var existingPayee = new Payee
        {
            Id = payeeId,
            UserId = Guid.NewGuid(),
            Name = "Test Payee"
        };
        var dbSet = A.Fake<DbSet<Payee>>();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(dbSet);
        A.CallTo(() => dbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(existingPayee);
        var logger = new FakeLogger<DeletePayee.Handler>();
        var request = new DeletePayee(payeeId);
        var handler = new DeletePayee.Handler(context, logger);

        // Act
        await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => dbSet.Remove(existingPayee)).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PayeeDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var dbSet = A.Fake<DbSet<Payee>>();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(dbSet);
        A.CallTo(() => dbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns((Payee?)null);
        var logger = new FakeLogger<DeletePayee.Handler>();
        var request = new DeletePayee(Guid.NewGuid());
        var handler = new DeletePayee.Handler(context, logger);
        var handle = new Func<Task>(() => handler.Handle(request, cancellationToken));

        // Act & Assert
        await handle.ShouldThrowAsync<NotFoundException<Payee>>();
    }
}
