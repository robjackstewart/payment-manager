using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;

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
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.Should().NotBeNull();
        var usersInDatabase = context.Users.Where(u => u.Name == userName).ToArray();
        usersInDatabase.Length.Should().Be(1);
        var user = usersInDatabase.First();
        user.Name.Should().Be(userName);
        user.Id.Should().NotBe(Guid.Empty);
        user.Name.Should().Be(body.Name);
        user.Id.Should().Be(body.Id);
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
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        body.Should().NotBeNull();
        body.Title.Should().Be("Invalid request");
        body.Detail.Should().Be("One or more validation errors occurred.");
        body.Errors.Count.Should().Be(1);
        body.Errors.Should().ContainKey("Name");
        body.Errors["Name"].Should().Contain("'Name' must not be empty.");
    }
}
