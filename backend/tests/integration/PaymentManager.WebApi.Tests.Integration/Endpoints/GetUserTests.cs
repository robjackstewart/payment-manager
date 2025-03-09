using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class GetUserTests
{
    private sealed record ExpectedResponse
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
    }

    [Test]
    public async Task GetUser_Should_Return_OkWithUser_When_UserExists()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test user"
        };
        context.Users.Add(user);
        await context.SaveChanges(cancellationToken);
        var expectedResponse = new ExpectedResponse
        {
            Id = user.Id,
            Name = user.Name
        };
        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/users/{user.Id}", cancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.ShouldBe(expectedResponse);
    }

    [Test]
    public async Task GetUser_Should_Return_NotFound_When_UserWithIdDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var client = applicationFactory.CreateClient();
        var userId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/users/{userId}", cancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        body.ShouldNotBeNull();
        body.Title.ShouldBe("User not found");
        body.Detail.ShouldBe($"User not found with criteria: Id is '{userId}'");

    }
}
