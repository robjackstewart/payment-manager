using FakeItEasy;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.WebApi.Endpoints;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Unit.Endpoints;

internal sealed class CreateUserEndpointTests
{
    [Test]
    public async Task Handle_Should_Return_CreatedResponse()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var request = new CreateUserEndpoint.Request(TestContext.CurrentContext.Random.GetString());
        var mediatRRequest = new CreateUser(request.Name);
        var sender = A.Fake<ISender>();
        A.CallTo(() => sender.Send(mediatRRequest, A<CancellationToken>._)).Returns(new CreateUser.Response(Guid.NewGuid(), request.Name));

        // Act
        var result = await CreateUserEndpoint.Handle(request, sender, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<CreateUser.Response>>();
        var createdResult = result as Microsoft.AspNetCore.Http.HttpResults.Created<CreateUser.Response>;
        createdResult.ShouldNotBeNull();
        createdResult.Value.ShouldNotBeNull();
        createdResult.Value.Name.ShouldBe(request.Name);
        createdResult.Location.ShouldBe($"/api/users/{createdResult.Value!.Id}");
    }
}
