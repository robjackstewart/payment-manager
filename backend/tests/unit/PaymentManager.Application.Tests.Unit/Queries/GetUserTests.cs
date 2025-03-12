using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PaymentManager.Application.Common;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetUserTests
{

    [Test]
    public async Task Handler_Handle_Should_Return_UserWithMatchIngId_When_UserWithMatchingIdExistsInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var matchingUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "User"
        };
        var nonMatchingUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Other User"
        };
        var users = new[] { matchingUser, nonMatchingUser };
        var usersDbSet = users.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        var logger = new FakeLogger<GetUser.Handler>();
        var request = new GetUser(matchingUser.Id);
        var handler = new GetUser.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(matchingUser.Id);
        result.Name.Should().Be(matchingUser.Name);
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowUsersNotFoundException_When_UserWithMatchingIdDoesNotExistInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var nonMatchingUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Other User"
        };
        var users = new[] { nonMatchingUser };
        var usersDbSet = users.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        var logger = new FakeLogger<GetUser.Handler>();
        var request = new GetUser(Guid.NewGuid());
        var handler = new GetUser.Handler(context, logger);
        var handle = new Func<Task>(() => handler.Handle(request, cancellationToken));

        // Act & Assert
        await handle.Should().ThrowAsync<NotFoundException<User>>();
    }
}
