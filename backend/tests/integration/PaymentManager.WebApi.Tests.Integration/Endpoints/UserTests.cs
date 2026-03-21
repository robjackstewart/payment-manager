using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class UserTests : IntegrationTestBase
{
    private sealed record CreateRequest(string Name);
    private sealed record UpdateRequest(string Name);
    private sealed record UserResponse(Guid Id, string Name);
    private sealed record GetAllResponse(UserResponse[] Users);

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreateUser_Should_Return_Created_With_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var client = CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/users", new CreateRequest("Alice"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.Name.ShouldBe("Alice");
    }

    [Test]
    public async Task CreateUser_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var client = CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/users", new CreateRequest(string.Empty), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Name");
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetUser_Should_Return_Ok_When_User_Exists()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "Bob" };
        context.Users.Add(user);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync($"/api/users/{user.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(user.Id);
        body.Name.ShouldBe(user.Name);
    }

    [Test]
    public async Task GetUser_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().GetAsync($"/api/users/{Guid.NewGuid()}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Title.ShouldBe("User not found");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllUsers_Should_Return_Ok_With_All_Users()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        context.Users.AddRange(
            new User { Id = Guid.NewGuid(), Name = "User1" },
            new User { Id = Guid.NewGuid(), Name = "User2" });
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync("/api/users", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.Users.Length.ShouldBe(3); // Includes the default user from IntegrationTestBase
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateUser_Should_Return_Ok_With_Updated_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "OriginalName" };
        context.Users.Add(user);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/users/{user.Id}", new UpdateRequest("UpdatedName"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(user.Id);
        body.Name.ShouldBe("UpdatedName");
    }

    [Test]
    public async Task UpdateUser_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", new UpdateRequest("NewName"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteUser_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "ToDelete" };
        context.Users.Add(user);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().DeleteAsync($"/api/users/{user.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
