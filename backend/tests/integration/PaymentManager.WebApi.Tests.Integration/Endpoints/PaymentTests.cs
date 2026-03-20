using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class PaymentTests : IntegrationTestBase
{
    private sealed record CreateRequest(
        Guid UserId, Guid PaymentSourceId, Guid PayeeId,
        decimal Amount, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate);

    private sealed record UpdateRequest(
        Guid UserId, Guid PaymentSourceId, Guid PayeeId,
        decimal Amount, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate);

    private sealed record PaymentResponse(
        Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId,
        decimal Amount, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate);

    private sealed record GetAllResponse(PaymentResponse[] Payments);

    private async Task<(Guid UserId, Guid PaymentSourceId, Guid PayeeId)> SetupPrerequisitesAsync(
        CancellationToken ct)
    {
        var context = GetService<IPaymentManagerContext>();
        var user = new User { Id = Guid.NewGuid(), Name = "TestUser" };
        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = user.Id, Name = "Visa" };
        var payee = new Payee { Id = Guid.NewGuid(), UserId = user.Id, Name = "Netflix" };
        context.Users.Add(user);
        context.PaymentSources.Add(paymentSource);
        context.Payees.Add(payee);
        await context.SaveChanges(ct);
        return (user.Id, paymentSource.Id, payee.Id);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePayment_Should_Return_Created_With_Payment()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (userId, paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            userId, paymentSourceId, payeeId, 9.99m, PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(userId);
        body.Amount.ShouldBe(9.99m);
        body.Frequency.ShouldBe(PaymentFrequency.Monthly);
    }

    [Test]
    public async Task CreatePayment_Should_Return_BadRequest_When_Amount_Is_Zero()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (userId, paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            userId, paymentSourceId, payeeId, 0m, PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Amount");
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPayment_Should_Return_Ok_When_Exists()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (userId, paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(), UserId = userId, PaymentSourceId = paymentSourceId,
            PayeeId = payeeId, Amount = 15.99m, Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1)
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync($"/api/payments/{payment.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(payment.Id);
        body.UserId.ShouldBe(userId);
        body.Amount.ShouldBe(15.99m);
        body.Frequency.ShouldBe(PaymentFrequency.Monthly);
    }

    [Test]
    public async Task GetPayment_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().GetAsync($"/api/payments/{Guid.NewGuid()}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Title.ShouldBe("Payment not found");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllPayments_Should_Return_Ok_With_Payments_For_User()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (userId, paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        context.Payments.AddRange(
            new Payment { Id = Guid.NewGuid(), UserId = userId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Amount = 10m, Frequency = PaymentFrequency.Monthly,  StartDate = new DateOnly(2025, 1, 1) },
            new Payment { Id = Guid.NewGuid(), UserId = userId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Amount = 20m, Frequency = PaymentFrequency.Annually,   StartDate = new DateOnly(2025, 1, 1) },
            new Payment { Id = Guid.NewGuid(), UserId = userId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Amount = 30m, Frequency = PaymentFrequency.Once,     StartDate = new DateOnly(2025, 6, 1) },
            new Payment { Id = Guid.NewGuid(), UserId = userId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Amount = 40m, Frequency = PaymentFrequency.Monthly,  StartDate = new DateOnly(2025, 3, 1) });
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync($"/api/payments?userId={userId}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.Payments.Length.ShouldBe(4);
        body.Payments.ShouldAllBe(p => p.UserId == userId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePayment_Should_Return_Ok_With_Updated_Payment()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (userId, paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(), UserId = userId, PaymentSourceId = paymentSourceId,
            PayeeId = payeeId, Amount = 9.99m, Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1)
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{payment.Id}", new UpdateRequest(
            userId, paymentSourceId, payeeId, 14.99m, PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Amount.ShouldBe(14.99m);
    }

    [Test]
    public async Task UpdatePayment_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (userId, paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{Guid.NewGuid()}", new UpdateRequest(
            userId, paymentSourceId, payeeId, 10m, PaymentFrequency.Once,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeletePayment_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (userId, paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(), UserId = userId, PaymentSourceId = paymentSourceId,
            PayeeId = payeeId, Amount = 50m, Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 1, 1)
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().DeleteAsync($"/api/payments/{payment.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
