using FakeItEasy;
using MediatR;
using NUnit.Framework;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Endpoints;
using FluentAssertions;

namespace PaymentManager.WebApi.Tests.Unit.Endpoints;

internal sealed class GetUserEndpointTests
{
    [Test]
    public async Task Handle_Should_Return_OkGetAllUsersResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var sender = A.Fake<ISender>();
        var userId = Guid.NewGuid();
        var request = new GetUser(userId);
        var mediatrResponse = new GetUser.Response(userId, TestContext.CurrentContext.Random.GetString());
        A.CallTo(() => sender.Send(request, A<CancellationToken>._)).Returns(mediatrResponse);

        // Act
        var result = await GetUserEndpoint.Handle(userId, sender, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetUser.Response>>();
        var createdResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetUser.Response>;
        createdResult.Should().NotBeNull();
        createdResult.Value.Should().NotBeNull();
        createdResult.Value.Should().BeEquivalentTo(mediatrResponse);
    }
}
