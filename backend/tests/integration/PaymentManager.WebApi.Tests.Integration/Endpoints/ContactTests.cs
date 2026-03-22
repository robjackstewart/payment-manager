using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using PaymentManager.WebApi.Services;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class ContactTests : IntegrationTestBase
{
    private sealed record CreateRequest(string Name);
    private sealed record UpdateRequest(string Name);
    private sealed record ContactResponse(Guid Id, Guid UserId, string Name);
    private sealed record GetAllResponse(ContactResponse[] Contacts);

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreateContact_Should_Return_Created_With_Contact()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/contacts", new CreateRequest("Alice"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ContactResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(DefaultUserService.DefaultUserId);
        body.Name.ShouldBe("Alice");
    }

    [Test]
    public async Task CreateContact_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/contacts", new CreateRequest(""), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Name");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllContacts_Should_Return_Ok_With_Contacts_For_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        await CreateApiClient().PostAsJsonAsync("/api/contacts", new CreateRequest("Bob"), ct);
        await CreateApiClient().PostAsJsonAsync("/api/contacts", new CreateRequest("Carol"), ct);

        var response = await CreateApiClient().GetAsync("/api/contacts", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.Contacts.Length.ShouldBe(2);
        body.Contacts.ShouldAllBe(c => c.UserId == DefaultUserService.DefaultUserId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateContact_Should_Return_Ok_With_Updated_Name()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/contacts", new CreateRequest("Dave"), ct))
            .Content.ReadFromJsonAsync<ContactResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().PutAsJsonAsync($"/api/contacts/{created.Id}", new UpdateRequest("David"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ContactResponse>(ct);
        body.ShouldNotBeNull();
        body.Name.ShouldBe("David");
    }

    [Test]
    public async Task UpdateContact_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PutAsJsonAsync($"/api/contacts/{Guid.NewGuid()}", new UpdateRequest("Nobody"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteContact_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/contacts", new CreateRequest("Eve"), ct))
            .Content.ReadFromJsonAsync<ContactResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().DeleteAsync($"/api/contacts/{created.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
