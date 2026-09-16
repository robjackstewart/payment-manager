using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using PaymentManager.WebApi.Services;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class PayerGroupTests : IntegrationTestBase
{
    private sealed record CreateRequest(string Name);
    private sealed record UpdateRequest(string Name);
    private sealed record SetMembersRequest(IReadOnlyList<Guid> PersonIds);
    private sealed record PayerGroupResponse(Guid Id, Guid UserId, string Name, Guid[] MemberPersonIds);
    private sealed record GetAllResponse(PayerGroupResponse[] PayerGroups);
    private sealed record SetMembersResponse(Guid PayerGroupId, Guid[] PersonIds);
    private sealed record PersonResponse(Guid Id, Guid UserId, string Name);

    private async Task<Guid> SetupPersonAsync(string name, CancellationToken ct)
    {
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/people", new { Name = name }, ct))
            .Content.ReadFromJsonAsync<PersonResponse>(ct);
        created.ShouldNotBeNull();
        return created.Id;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePayerGroup_Should_Return_Created_With_PayerGroup()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Family"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PayerGroupResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(DefaultUserService.DefaultUserId);
        body.Name.ShouldBe("Family");
    }

    [Test]
    public async Task CreatePayerGroup_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest(""), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Name");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllPayerGroups_Should_Return_Ok_With_PayerGroups_For_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Family"), ct);
        await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Housemates"), ct);

        var response = await CreateApiClient().GetAsync("/api/payer-groups", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.PayerGroups.Length.ShouldBe(2);
        body.PayerGroups.ShouldAllBe(g => g.UserId == DefaultUserService.DefaultUserId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePayerGroup_Should_Return_Ok_With_Updated_Name()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Old Name"), ct))
            .Content.ReadFromJsonAsync<PayerGroupResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payer-groups/{created.Id}", new UpdateRequest("New Name"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PayerGroupResponse>(ct);
        body.ShouldNotBeNull();
        body.Name.ShouldBe("New Name");
    }

    [Test]
    public async Task UpdatePayerGroup_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payer-groups/{Guid.NewGuid()}", new UpdateRequest("Nobody"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Members ───────────────────────────────────────────────────────────────

    [Test]
    public async Task SetMembers_Should_Return_Ok_With_UpdatedMembership()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Family"), ct))
            .Content.ReadFromJsonAsync<PayerGroupResponse>(ct);
        created.ShouldNotBeNull();
        var personId1 = await SetupPersonAsync("Alice", ct);
        var personId2 = await SetupPersonAsync("Bob", ct);

        var response = await CreateApiClient().PutAsJsonAsync(
            $"/api/payer-groups/{created.Id}/members", new SetMembersRequest([personId1, personId2]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SetMembersResponse>(ct);
        body.ShouldNotBeNull();
        body.PersonIds.ShouldBe([personId1, personId2], ignoreOrder: true);

        var getResponse = await CreateApiClient().GetAsync("/api/payer-groups", ct);
        var getBody = await getResponse.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        getBody.ShouldNotBeNull();
        getBody.PayerGroups.Single(g => g.Id == created.Id).MemberPersonIds.ShouldBe([personId1, personId2], ignoreOrder: true);
    }

    [Test]
    public async Task SetMembers_Should_Return_BadRequest_When_RemovingMemberWhoStillHoldsASplit()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Family"), ct))
            .Content.ReadFromJsonAsync<PayerGroupResponse>(ct);
        created.ShouldNotBeNull();
        var personId = await SetupPersonAsync("Alice", ct);
        await CreateApiClient().PutAsJsonAsync($"/api/payer-groups/{created.Id}/members", new SetMembersRequest([personId]), ct);

        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Visa" };
        var payee = new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Landlord" };
        context.PaymentSources.Add(paymentSource);
        context.Payees.Add(payee);
        await context.SaveChanges(ct);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSource.Id,
            PayeeId = payee.Id,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 100m,
            PayerGroupId = created.Id
        };
        context.Payments.Add(payment);
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment.Id, PersonId = personId, Percentage = 100m });
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync(
            $"/api/payer-groups/{created.Id}/members", new SetMembersRequest([]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeletePayerGroup_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Disposable"), ct))
            .Content.ReadFromJsonAsync<PayerGroupResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().DeleteAsync($"/api/payer-groups/{created.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeletePayerGroup_Should_Return_BadRequest_When_GroupStillHasPayments()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payer-groups", new CreateRequest("Family"), ct))
            .Content.ReadFromJsonAsync<PayerGroupResponse>(ct);
        created.ShouldNotBeNull();

        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Visa" };
        var payee = new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Landlord" };
        context.PaymentSources.Add(paymentSource);
        context.Payees.Add(payee);
        await context.SaveChanges(ct);
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSource.Id,
            PayeeId = payee.Id,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 100m,
            PayerGroupId = created.Id
        });
        await context.SaveChanges(ct);

        var response = await CreateApiClient().DeleteAsync($"/api/payer-groups/{created.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
