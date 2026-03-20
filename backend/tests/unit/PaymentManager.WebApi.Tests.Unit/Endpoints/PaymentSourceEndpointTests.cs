using FakeItEasy;
using MediatR;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Queries;
using PaymentManager.WebApi.Endpoints;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Unit.Endpoints;

internal sealed class PaymentSourceEndpointTests
{
    [Test]
    public async Task HandleCreate_Should_Return_CreatedResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var name = TestContext.CurrentContext.Random.GetString();
        var request = new PaymentSourceEndpoints.CreateRequest(userId, name);
        var responseId = Guid.NewGuid();
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(A<CreatePaymentSource>._, A<CancellationToken>._))
            .Returns(new CreatePaymentSource.Response(responseId, userId, name));

        // Act
        var result = await PaymentSourceEndpoints.HandleCreate(request, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<CreatePaymentSource.Response>>();
        var created = result as Microsoft.AspNetCore.Http.HttpResults.Created<CreatePaymentSource.Response>;
        created.ShouldNotBeNull();
        created.Value.ShouldNotBeNull();
        created.Value!.Id.ShouldBe(responseId);
        created.Value.UserId.ShouldBe(userId);
        created.Value.Name.ShouldBe(name);
        created.Location.ShouldBe($"/api/payment-sources/{responseId}");
    }

    [Test]
    public async Task HandleGetAll_Should_Return_OkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var sender = A.Fake<ISender>();
        var paymentSources = new List<GetAllPaymentSources.Response.PaymentSourceDto>
        {
            new(Guid.NewGuid(), userId, TestContext.CurrentContext.Random.GetString()),
            new(Guid.NewGuid(), userId, TestContext.CurrentContext.Random.GetString())
        };
        A.CallTo(() => sender.Send(A<GetAllPaymentSources>._, A<CancellationToken>._))
            .Returns(new GetAllPaymentSources.Response(paymentSources));

        // Act
        var result = await PaymentSourceEndpoints.HandleGetAll(userId, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllPaymentSources.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetAllPaymentSources.Response>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.PaymentSources.Count.ShouldBe(2);
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
        A.CallTo(() => sender.Send(A<GetPaymentSource>._, A<CancellationToken>._))
            .Returns(new GetPaymentSource.Response(id, userId, name));

        // Act
        var result = await PaymentSourceEndpoints.HandleGet(id, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<GetPaymentSource.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetPaymentSource.Response>;
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
        var request = new PaymentSourceEndpoints.UpdateRequest(userId, name);
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(A<UpdatePaymentSource>._, A<CancellationToken>._))
            .Returns(new UpdatePaymentSource.Response(id, userId, name));

        // Act
        var result = await PaymentSourceEndpoints.HandleUpdate(id, request, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<UpdatePaymentSource.Response>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<UpdatePaymentSource.Response>;
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
        A.CallTo(() => sender.Send(A<DeletePaymentSource>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        // Act
        var result = await PaymentSourceEndpoints.HandleDelete(id, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }
}
