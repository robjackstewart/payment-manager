using FakeItEasy;
using MediatR;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Endpoints;
using PaymentManager.WebApi.Services;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Unit.Endpoints;

internal sealed class PayeeEndpointTests
{
    private static IUserService CreateUserService(Guid userId)
    {
        var userService = A.Fake<IUserService>();
        A.CallTo(() => userService.GetCurrentUserId()).Returns(userId);
        return userService;
    }

    [Test]
    public async Task HandleCreate_Should_Return_CreatedResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var name = TestContext.CurrentContext.Random.GetString();
        var request = new PayeeEndpoints.CreateRequest(name);
        var responseId = Guid.NewGuid();
        var sender = A.Fake<ISender>();
        var userService = CreateUserService(userId);
        A.CallTo(() => sender.Send(A<CreatePayee>._, A<CancellationToken>._))
            .Returns(new CreatePayee.Response(responseId, userId, name));

        // Act
        var result = await PayeeEndpoints.HandleCreate(request, sender, userService, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<CreatePayee.Response>>();
        var created = result as Microsoft.AspNetCore.Http.HttpResults.Created<CreatePayee.Response>;
        created.ShouldNotBeNull();
        created.Value.ShouldNotBeNull();
        created.Value!.Id.ShouldBe(responseId);
        created.Value.UserId.ShouldBe(userId);
        created.Value.Name.ShouldBe(name);
        created.Location.ShouldBe($"/api/payees/{responseId}");
    }

    [Test]
    public async Task HandleGetAll_Should_Return_OkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var sender = A.Fake<ISender>();
        var userService = CreateUserService(userId);
        var payees = new List<GetAllPayees.Response.PayeeDto>
        {
            new(Guid.NewGuid(), userId, TestContext.CurrentContext.Random.GetString()),
            new(Guid.NewGuid(), userId, TestContext.CurrentContext.Random.GetString())
        };
        A.CallTo(() => sender.Send(A<GetAllPayees>._, A<CancellationToken>._))
            .Returns(new GetAllPayees.Response(payees));

        // Act
        var result = await PayeeEndpoints.HandleGetAll(sender, userService, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllPayees.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllPayees.Response>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.Payees.Count.ShouldBe(2);
    }

    [Test]
    public async Task HandleGet_Should_Return_OkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = TestContext.CurrentContext.Random.GetString();
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(A<GetPayee>._, A<CancellationToken>._))
            .Returns(new GetPayee.Response(id, userId, name));

        // Act
        var result = await PayeeEndpoints.HandleGet(id, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetPayee.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetPayee.Response>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.Id.ShouldBe(id);
        ok.Value.UserId.ShouldBe(userId);
        ok.Value.Name.ShouldBe(name);
    }

    [Test]
    public async Task HandleUpdate_Should_Return_OkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var name = TestContext.CurrentContext.Random.GetString();
        var request = new PayeeEndpoints.UpdateRequest(name);
        var sender = A.Fake<ISender>();
        var userService = CreateUserService(userId);
        A.CallTo(() => sender.Send(A<UpdatePayee>._, A<CancellationToken>._))
            .Returns(new UpdatePayee.Response(id, userId, name));

        // Act
        var result = await PayeeEndpoints.HandleUpdate(id, request, sender, userService, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<UpdatePayee.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<UpdatePayee.Response>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.Id.ShouldBe(id);
        ok.Value.UserId.ShouldBe(userId);
        ok.Value.Name.ShouldBe(name);
    }

    [Test]
    public async Task HandleDelete_Should_Return_NoContentResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var id = Guid.NewGuid();
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(A<DeletePayee>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        // Act
        var result = await PayeeEndpoints.HandleDelete(id, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }
}