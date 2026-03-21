using NUnit.Framework;
using PaymentManager.Application.Commands;
using FakeItEasy;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class DeletePaymentSourceTests
{
    [Test]
    public async Task Handler_Handle_Should_DeletePaymentSourceFromContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existingPaymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Test source" };
        var context = A.Fake<IPaymentManagerContext>();
        var dbSet = A.Fake<DbSet<PaymentSource>>();
        A.CallTo(() => context.PaymentSources).Returns(dbSet);
        A.CallTo(() => dbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(existingPaymentSource);
        var logger = new FakeLogger<DeletePaymentSource.Handler>();
        var request = new DeletePaymentSource(existingPaymentSource.Id);
        var handler = new DeletePaymentSource.Handler(context, logger);

        // Act
        await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => context.PaymentSources.Remove(A<PaymentSource>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PaymentSourceDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var dbSet = A.Fake<DbSet<PaymentSource>>();
        A.CallTo(() => context.PaymentSources).Returns(dbSet);
        A.CallTo(() => dbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns((PaymentSource?)null);
        var logger = new FakeLogger<DeletePaymentSource.Handler>();
        var request = new DeletePaymentSource(Guid.NewGuid());
        var handler = new DeletePaymentSource.Handler(context, logger);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<PaymentSource>>(() => handler.Handle(request, cancellationToken));
    }
}
