using FakeItEasy;
using MediatR;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Endpoints;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Unit.Endpoints;

internal sealed class UserEndpointTests
{
    [Test]
    public async Task HandleCreate_Should_Return_CreatedResponse()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var sender = A.Fake<ISender>();
        var request = new UserEndpoints.CreateRequest("Test User");
        var userId = Guid.NewGuid();
        var mediatrResponse = new CreateUser.Response(userId, "Test User");
        A.CallTo(() => sender.Send(A<CreateUser>._, A<CancellationToken>._)).Returns(mediatrResponse);

        var result = await UserEndpoints.HandleCreate(request, sender, cancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<CreateUser.Response>>();
        var createdResult = result as Microsoft.AspNetCore.Http.HttpResults.Created<CreateUser.Response>;
        createdResult.ShouldNotBeNull();
        createdResult.Location.ShouldBe($"/api/users/{userId}");
        createdResult.Value.ShouldNotBeNull();
        createdResult.Value.ShouldBeEquivalentTo(mediatrResponse);
    }

    [Test]
    public async Task HandleGetAll_Should_Return_OkResponse()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var sender = A.Fake<ISender>();
        var users = new List<GetAllUsers.Response.UserDto>
        {
            new(Guid.NewGuid(), "User 1"),
            new(Guid.NewGuid(), "User 2"),
        };
        var mediatrResponse = new GetAllUsers.Response(users);
        A.CallTo(() => sender.Send(A<GetAllUsers>._, A<CancellationToken>._)).Returns(mediatrResponse);

        var result = await UserEndpoints.HandleGetAll(sender, cancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllUsers.Response>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllUsers.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ShouldBeEquivalentTo(mediatrResponse);
    }

    [Test]
    public async Task HandleGet_Should_Return_OkResponse()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var sender = A.Fake<ISender>();
        var userId = Guid.NewGuid();
        var mediatrResponse = new GetUser.Response(userId, TestContext.CurrentContext.Random.GetString());
        A.CallTo(() => sender.Send(A<GetUser>._, A<CancellationToken>._)).Returns(mediatrResponse);

        var result = await UserEndpoints.HandleGet(userId, sender, cancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetUser.Response>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetUser.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ShouldBeEquivalentTo(mediatrResponse);
    }

    [Test]
    public async Task HandleUpdate_Should_Return_OkResponse()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var sender = A.Fake<ISender>();
        var userId = Guid.NewGuid();
        var request = new UserEndpoints.UpdateRequest("Updated Name");
        var mediatrResponse = new UpdateUser.Response(userId, "Updated Name");
        A.CallTo(() => sender.Send(A<UpdateUser>._, A<CancellationToken>._)).Returns(mediatrResponse);

        var result = await UserEndpoints.HandleUpdate(userId, request, sender, cancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<UpdateUser.Response>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<UpdateUser.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ShouldBeEquivalentTo(mediatrResponse);
    }

    [Test]
    public async Task HandleDelete_Should_Return_NoContentResponse()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var sender = A.Fake<ISender>();
        var userId = Guid.NewGuid();
        var result = await UserEndpoints.HandleDelete(userId, sender, cancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }
}
