using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class GetAllUsersTests
{
    private sealed record ExpectedUserDtoResponse(Guid Id, string Name);

    private record ExpectedResponse(ExpectedUserDtoResponse[] Users);


    [Test]
    public async Task GetAllUsers_Should_Return_OkWithAllUsersInContext_When_UsersExists()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        using var applicationFactory = new PaymentMangerWebApiWebApplicationFactory();
        var context = applicationFactory.Services.GetRequiredService<IPaymentManagerContext>();
        var users = new[]{
            new User
            {
                Id = Guid.NewGuid(),
                Name = "User1"
            },
            new User
            {
                Id = Guid.NewGuid(),
                Name = "User2"
            }
        };
        context.Users.AddRange(users);
        await context.SaveChanges(cancellationToken);
        var expectedUsersResponse = users.OrderBy(u => u.Id).Select(u => new ExpectedUserDtoResponse(u.Id, u.Name)).ToArray();
        var expectedResponse = new ExpectedResponse(expectedUsersResponse);
        var client = applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/users", cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExpectedResponse>();
        body.Should().NotBeNull();
        body.Should().BeEquivalentTo(expectedResponse);
    }
}
