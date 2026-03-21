using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class PaymentSourceTests : IntegrationTestBase
{
    private sealed record CreateRequest(Guid UserId, string Name);
    private sealed record UpdateRequest(Guid UserId, string Name);
    private sealed record PaymentSourceResponse(Guid Id, Guid UserId, string Name);
    private sealed record GetAllResponse(PaymentSourceResponse[] PaymentSources);

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePaymentSource_Should_Return_Created_With_PaymentSource()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "Alice" };
        context.Users.Add(user);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payment-sources",
            new CreateRequest(user.Id, "New Visa Card"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentSourceResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(user.Id);
        body.Name.ShouldBe("New Visa Card");
    }

    [Test]
    public async Task CreatePaymentSource_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "Alice" };
        context.Users.Add(user);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payment-sources",
            new CreateRequest(user.Id, string.Empty), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Name");
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPaymentSource_Should_Return_Ok_When_Exists()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "Alice" };
        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = user.Id, Name = "Alice Visa" };
        context.Users.Add(user);
        context.PaymentSources.Add(paymentSource);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync($"/api/payment-sources/{paymentSource.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentSourceResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(paymentSource.Id);
        body.UserId.ShouldBe(user.Id);
    }

    [Test]
    public async Task GetPaymentSource_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().GetAsync($"/api/payment-sources/{Guid.NewGuid()}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Title.ShouldBe("PaymentSource not found");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllPaymentSources_Should_Return_Ok_With_Sources_For_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "Alice" };
        context.Users.Add(user);
        context.PaymentSources.AddRange(
            new PaymentSource { Id = Guid.NewGuid(), UserId = user.Id, Name = "Visa" },
            new PaymentSource { Id = Guid.NewGuid(), UserId = user.Id, Name = "Debit" });
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync($"/api/payment-sources?userId={user.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.PaymentSources.Length.ShouldBe(2);
        body.PaymentSources.ShouldAllBe(ps => ps.UserId == user.Id);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePaymentSource_Should_Return_Ok_With_Updated_Source()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "Alice" };
        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = user.Id, Name = "Alice Visa" };
        context.Users.Add(user);
        context.PaymentSources.Add(paymentSource);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payment-sources/{paymentSource.Id}",
            new UpdateRequest(user.Id, "Updated Visa"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentSourceResponse>(ct);
        body.ShouldNotBeNull();
        body.Name.ShouldBe("Updated Visa");
    }

    [Test]
    public async Task UpdatePaymentSource_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payment-sources/{Guid.NewGuid()}",
            new UpdateRequest(Guid.NewGuid(), "Name"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeletePaymentSource_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "Alice" };
        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = user.Id, Name = "Alice Debit" };
        context.Users.Add(user);
        context.PaymentSources.Add(paymentSource);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().DeleteAsync($"/api/payment-sources/{paymentSource.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
