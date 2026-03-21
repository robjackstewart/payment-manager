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

internal sealed class DeleteUserTests
{
    [Test]
    public async Task Handler_Handle_Should_DeleteUserFromContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existingUser = new User { Id = Guid.NewGuid(), Name = "Test name" };
        var context = A.Fake<IPaymentManagerContext>();
        var usersDbSet = A.Fake<DbSet<User>>();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        A.CallTo(() => usersDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(existingUser);
        var logger = new FakeLogger<DeleteUser.Handler>();
        var request = new DeleteUser(existingUser.Id);
        var handler = new DeleteUser.Handler(context, logger);

        // Act
        await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => context.Users.Remove(A<User>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_UserDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var usersDbSet = A.Fake<DbSet<User>>();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        A.CallTo(() => usersDbSet.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns((User?)null);
        var logger = new FakeLogger<DeleteUser.Handler>();
        var request = new DeleteUser(Guid.NewGuid());
        var handler = new DeleteUser.Handler(context, logger);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<User>>(() => handler.Handle(request, cancellationToken));
    }
}
