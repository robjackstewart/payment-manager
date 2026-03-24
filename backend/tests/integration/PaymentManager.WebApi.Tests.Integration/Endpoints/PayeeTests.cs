using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.WebApi.Services;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class PayeeTests : IntegrationTestBase
{
    private sealed record CreateRequest(string Name);
    private sealed record UpdateRequest(string Name);
    private sealed record PayeeResponse(Guid Id, Guid UserId, string Name);
    private sealed record GetAllResponse(PayeeResponse[] Payees);

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePayee_Should_Return_Created_With_Payee()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/payees",
            new CreateRequest("New Payee"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PayeeResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(DefaultUserService.DefaultUserId);
        body.Name.ShouldBe("New Payee");
    }

    [Test]
    public async Task CreatePayee_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/payees",
            new CreateRequest(string.Empty), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Name");
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPayee_Should_Return_Ok_When_Exists()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var payee = new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Netflix" };
        context.Payees.Add(payee);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync($"/api/payees/{payee.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PayeeResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(payee.Id);
        body.Name.ShouldBe("Netflix");
    }

    [Test]
    public async Task GetPayee_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().GetAsync($"/api/payees/{Guid.NewGuid()}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Title.ShouldBe("Payee not found");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllPayees_Should_Return_Ok_With_Payees_For_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        context.Payees.AddRange(
            new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Netflix" },
            new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Landlord" },
            new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Electric" },
            new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Gym" });
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync("/api/payees", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.Payees.Length.ShouldBe(4);
        body.Payees.ShouldAllBe(p => p.UserId == DefaultUserService.DefaultUserId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePayee_Should_Return_Ok_With_Updated_Payee()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var payee = new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Spotify" };
        context.Payees.Add(payee);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payees/{payee.Id}",
            new UpdateRequest("Spotify Premium"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PayeeResponse>(ct);
        body.ShouldNotBeNull();
        body.Name.ShouldBe("Spotify Premium");
    }

    [Test]
    public async Task UpdatePayee_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payees/{Guid.NewGuid()}",
            new UpdateRequest("Name"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeletePayee_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var payee = new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Dentist" };
        context.Payees.Add(payee);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().DeleteAsync($"/api/payees/{payee.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}