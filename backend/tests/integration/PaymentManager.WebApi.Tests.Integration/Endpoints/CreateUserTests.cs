using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class CreateUserTests
{

    private sealed record ExpectedRequest
    {
        public required string Name { get; init; }
    }

    private sealed record ExpectedResponse
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
    }

    [Test]
    public async Task CreateUser_Should_Return_OkWithCreatedUser()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userName = "Test user";
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/user", new ExpectedRequest
        {
            Name = userName,
        }, cancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.ShouldNotBeNull();
        var usersInDatabase = context.Users.Where(u => u.Name == userName).ToArray();
        usersInDatabase.Length.ShouldBe(1);
        var user = usersInDatabase.First();
        user.Name.ShouldBe(userName);
        user.Id.ShouldNotBe(Guid.Empty);
        user.Name.ShouldBe(body.Name);
        user.Id.ShouldBe(body.Id);
    }

    [Test]
    public async Task CreateUser_Should_Return_BadRequestWithErrors_When_NameIsEmpty()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/user", new
        {
            Name = string.Empty,
        }, cancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        body.ShouldNotBeNull();
        body.Title.ShouldBe("Invalid request");
        body.Detail.ShouldBe("One or more validation errors occurred.");
        body.Errors.Count.ShouldBe(1);
        body.Errors.ShouldContainKey("Name");
        body.Errors["Name"].ShouldContain("'Name' must not be empty.");
    }
}
