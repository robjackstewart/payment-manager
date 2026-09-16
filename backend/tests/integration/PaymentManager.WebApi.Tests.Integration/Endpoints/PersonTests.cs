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

internal sealed class PersonTests : IntegrationTestBase
{
    private sealed record CreateRequest(string Name);
    private sealed record UpdateRequest(string Name);
    private sealed record PersonResponse(Guid Id, Guid UserId, string Name);
    private sealed record GetAllResponse(PersonResponse[] People);

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePerson_Should_Return_Created_With_Person()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/people", new CreateRequest("Alice"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PersonResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(DefaultUserService.DefaultUserId);
        body.Name.ShouldBe("Alice");
    }

    [Test]
    public async Task CreatePerson_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync("/api/people", new CreateRequest(""), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Name");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllPeople_Should_Return_Ok_With_People_For_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        await CreateApiClient().PostAsJsonAsync("/api/people", new CreateRequest("Bob"), ct);
        await CreateApiClient().PostAsJsonAsync("/api/people", new CreateRequest("Carol"), ct);

        var response = await CreateApiClient().GetAsync("/api/people", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.People.Length.ShouldBe(2);
        body.People.ShouldAllBe(p => p.UserId == DefaultUserService.DefaultUserId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePerson_Should_Return_Ok_With_Updated_Name()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/people", new CreateRequest("Dave"), ct))
            .Content.ReadFromJsonAsync<PersonResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().PutAsJsonAsync($"/api/people/{created.Id}", new UpdateRequest("David"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PersonResponse>(ct);
        body.ShouldNotBeNull();
        body.Name.ShouldBe("David");
    }

    [Test]
    public async Task UpdatePerson_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PutAsJsonAsync($"/api/people/{Guid.NewGuid()}", new UpdateRequest("Nobody"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeletePerson_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var created = await (await CreateApiClient().PostAsJsonAsync("/api/people", new CreateRequest("Eve"), ct))
            .Content.ReadFromJsonAsync<PersonResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().DeleteAsync($"/api/people/{created.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeletePerson_Should_Return_BadRequest_When_PersonHoldsSplits()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var context = GetService<IPaymentManagerContext>();
        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Visa" };
        var payee = new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Landlord" };
        var person = new Person { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Alice" };
        context.PaymentSources.Add(paymentSource);
        context.Payees.Add(payee);
        context.People.Add(person);
        await context.SaveChanges(ct);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSource.Id,
            PayeeId = payee.Id,
            Currency = "USD",
            Frequency = PaymentManager.Domain.Enums.PaymentFrequency.Monthly,
            Direction = PaymentManager.Domain.Enums.PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 100m
        };
        context.Payments.Add(payment);
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment.Id, PersonId = person.Id, Percentage = 100m });
        await context.SaveChanges(ct);

        var response = await CreateApiClient().DeleteAsync($"/api/people/{person.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
