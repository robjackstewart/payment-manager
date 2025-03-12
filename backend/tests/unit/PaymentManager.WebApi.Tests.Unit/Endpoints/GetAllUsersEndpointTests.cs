using FakeItEasy;
using MediatR;
using NUnit.Framework;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Endpoints;
using FluentAssertions;

namespace PaymentManager.WebApi.Tests.Unit.Endpoints;

public class GetAllUsersEndpointTests
{
    [Test]
    public async Task Handle_Should_Return_OkGetAllUsersResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var sender = A.Fake<ISender>();
        var mediatrResponse = new GetAllUsers.Response([
            new GetAllUsers.Response.UserDto(Guid.NewGuid(), TestContext.CurrentContext.Random.GetString()),
            new GetAllUsers.Response.UserDto(Guid.NewGuid(), TestContext.CurrentContext.Random.GetString())
        ]);
        A.CallTo(() => sender.Send(A<GetAllUsers>._, A<CancellationToken>._)).Returns(mediatrResponse);

        // Act
        var result = await GetAllUsersEndpoint.Handle(sender, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllUsers.Response>>();
        var createdResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllUsers.Response>;
        createdResult.Should().NotBeNull();
        createdResult.Value.Should().NotBeNull();
        createdResult.Value.Should().BeEquivalentTo(mediatrResponse);
    }
}
