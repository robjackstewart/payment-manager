using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using PaymentManager.Application.Common;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using Shouldly;
using static PaymentManager.Application.Queries.GetAllUsers.Response;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetAllUsersTests
{
    [Test]
    public async Task Handle_Should_Return_AllUsersInContextAsDto()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var request = new GetAllUsers();
        var users = new[] {
            new User { Id = Guid.NewGuid(), Name = "User1" },
            new User { Id = Guid.NewGuid(), Name = "User2" },
            new User { Id = Guid.NewGuid(), Name = "User3" }
        };
        var usersDbSet = users.BuildMockDbSet();
        var expectedUsers = users.OrderBy(u => u.Id).Select(u => new UserDto(u.Id, u.Name)).ToArray();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        var logger = new FakeLogger<GetAllUsers.Handler>();
        var handler = new GetAllUsers.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Users.ShouldBe(expectedUsers);
    }
}
