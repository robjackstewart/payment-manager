using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using PaymentManager.WebApi.Services;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class PaymentTests : IntegrationTestBase
{
    private sealed record CreateRequest(
        Guid PaymentSourceId, Guid PayeeId,
        decimal Amount, string Currency, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate, string? Description = null,
        IReadOnlyList<SplitRequest>? Splits = null);

    private sealed record UpdateRequest(
        Guid PaymentSourceId, Guid PayeeId,
        decimal InitialAmount, string Currency, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate, string? Description = null,
        IReadOnlyList<SplitRequest>? Splits = null);

    private sealed record SplitRequest(Guid ContactId, decimal Percentage);

    private sealed record PaymentResponse(
        Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId,
        decimal CurrentAmount, string Currency, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate, string? Description,
        UserShareDto UserShare, SplitDto[] Splits, ValueDto[] Values);

    private sealed record SplitDto(Guid ContactId, decimal Percentage, decimal Value);

    private sealed record ValueDto(DateOnly EffectiveDate, decimal Amount);

    private sealed record GetAllResponse(PaymentResponse[] Payments);

    private async Task<(Guid PaymentSourceId, Guid PayeeId)> SetupPrerequisitesAsync(CancellationToken ct)
    {
        var context = GetService<IPaymentManagerContext>();
        var paymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Visa" };
        var payee = new Payee { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Netflix" };
        context.PaymentSources.Add(paymentSource);
        context.Payees.Add(payee);
        await context.SaveChanges(ct);
        return (paymentSource.Id, payee.Id);
    }

    private async Task<Guid> SetupContactAsync(string name, CancellationToken ct)
    {
        var context = GetService<IPaymentManagerContext>();
        var contact = new Contact { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = name };
        context.Contacts.Add(contact);
        await context.SaveChanges(ct);
        return contact.Id;
    }

    private static void AddEffectiveValue(IPaymentManagerContext context, Guid paymentId, DateOnly effectiveDate, decimal amount)
    {
        context.EffectivePaymentValues.Add(new EffectivePaymentValue
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            EffectiveDate = effectiveDate,
            Amount = amount
        });
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePayment_Should_Return_Created_With_Payment()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(DefaultUserService.DefaultUserId);
        body.CurrentAmount.ShouldBe(9.99m);
        body.Frequency.ShouldBe(PaymentFrequency.Monthly);
        body.UserShare.Percentage.ShouldBe(100m);
        body.UserShare.Value.ShouldBe(9.99m);
    }

    [Test]
    public async Task CreatePayment_Should_Return_BadRequest_When_Amount_Is_Zero()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 0m, "USD", PaymentFrequency.Monthly,
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
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 15.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync($"/api/payments/{payment.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(payment.Id);
        body.UserId.ShouldBe(DefaultUserService.DefaultUserId);
        body.CurrentAmount.ShouldBe(15.99m);
        body.Frequency.ShouldBe(PaymentFrequency.Monthly);
        body.UserShare.Percentage.ShouldBe(100m);
        body.UserShare.Value.ShouldBe(15.99m);
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
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payments = new[]
        {
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Monthly, StartDate = new DateOnly(2025, 1, 1), InitialAmount = 10m },
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Annually, StartDate = new DateOnly(2025, 1, 1), InitialAmount = 10m },
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Once, StartDate = new DateOnly(2025, 6, 1), InitialAmount = 10m },
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Monthly, StartDate = new DateOnly(2025, 3, 1), InitialAmount = 10m }
        };
        context.Payments.AddRange(payments);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().GetAsync("/api/payments", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);
        body.ShouldNotBeNull();
        body.Payments.Length.ShouldBe(4);
        body.Payments.ShouldAllBe(p => p.UserId == DefaultUserService.DefaultUserId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePayment_Should_Return_Ok_With_Updated_Payment()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{payment.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 9.99m, "EUR", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.CurrentAmount.ShouldBe(9.99m);
        body.Currency.ShouldBe("EUR");
        body.UserShare.Percentage.ShouldBe(100m);
        body.UserShare.Value.ShouldBe(9.99m);
    }

    [Test]
    public async Task UpdatePayment_Should_Update_InitialAmount()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{payment.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 14.99m, "EUR", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.CurrentAmount.ShouldBe(14.99m);
        body.Currency.ShouldBe("EUR");
        body.Values.Length.ShouldBe(0);
    }

    [Test]
    public async Task UpdatePayment_Should_Return_NotFound_When_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{Guid.NewGuid()}", new UpdateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Once,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeletePayment_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 50m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().DeleteAsync($"/api/payments/{payment.Id}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CreatePayment_Should_Return_Description_When_Provided()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, "My streaming service"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Description.ShouldBe("My streaming service");
        body.UserShare.Percentage.ShouldBe(100m);
        body.UserShare.Value.ShouldBe(9.99m);
    }

    [Test]
    public async Task UpdatePayment_Should_Return_Updated_Description()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
            Description = "Original description",
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{payment.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, "Updated description"), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Description.ShouldBe("Updated description");
        body.UserShare.Percentage.ShouldBe(100m);
        body.UserShare.Value.ShouldBe(9.99m);
    }

    // ── Splits ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePayment_Should_Return_Splits_When_Provided()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var contactId = await SetupContactAsync("Derek", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null,
            [new SplitRequest(contactId, 30m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Splits.Length.ShouldBe(1);
        body.Splits[0].ContactId.ShouldBe(contactId);
        body.Splits[0].Percentage.ShouldBe(30m);
        body.Splits[0].Value.ShouldBe(30m);
        body.UserShare.Percentage.ShouldBe(70m);
        body.UserShare.Value.ShouldBe(70m);
    }

    [Test]
    public async Task CreatePayment_Should_Return_BadRequest_When_Splits_Exceed_100_Percent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var contactId1 = await SetupContactAsync("Alice", ct);
        var contactId2 = await SetupContactAsync("Bob", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null,
            [new SplitRequest(contactId1, 60m), new SplitRequest(contactId2, 50m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreatePayment_Should_Return_NotFound_When_ContactId_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null,
            [new SplitRequest(Guid.NewGuid(), 30m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdatePayment_Should_Replace_Splits()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var contactId1 = await SetupContactAsync("Liz", ct);
        var contactId2 = await SetupContactAsync("Sam", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null,
            [new SplitRequest(contactId1, 25m)]), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();
        created.Splits.Length.ShouldBe(1);

        var updateResponse = await CreateApiClient().PutAsJsonAsync($"/api/payments/{created.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null,
            [new SplitRequest(contactId2, 40m)]), ct);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResponse.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Splits.Length.ShouldBe(1);
        body.Splits[0].ContactId.ShouldBe(contactId2);
        body.Splits[0].Percentage.ShouldBe(40m);
        body.Splits[0].Value.ShouldBe(40m);
        body.UserShare.Percentage.ShouldBe(60m);
        body.UserShare.Value.ShouldBe(60m);
    }

    [Test]
    public async Task GetAllPayments_Should_Include_Splits()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var contactId = await SetupContactAsync("Frank", ct);
        await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 50m, "USD", PaymentFrequency.Once,
            new DateOnly(2026, 1, 1), null, null,
            [new SplitRequest(contactId, 50m)]), ct);

        var response = await CreateApiClient().GetAsync("/api/payments", ct);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);

        body.ShouldNotBeNull();
        body.Payments.Length.ShouldBe(1);
        body.Payments[0].Splits.Length.ShouldBe(1);
        body.Payments[0].Splits[0].ContactId.ShouldBe(contactId);
        body.Payments[0].Splits[0].Percentage.ShouldBe(50m);
        body.Payments[0].Splits[0].Value.ShouldBe(25m);
        body.Payments[0].UserShare.Percentage.ShouldBe(50m);
        body.Payments[0].UserShare.Value.ShouldBe(25m);
    }

    // ── AddPaymentValue ───────────────────────────────────────────────────────

    [Test]
    public async Task AddPaymentValue_Should_Return_Created_With_Value()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/values",
            new { effectiveDate = "2026-01-01", amount = 12.99m }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Test]
    public async Task AddPaymentValue_Should_Update_When_Date_Already_Exists()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        // Upsert the same date with a new amount
        var response = await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/values",
            new { effectiveDate = "2026-01-01", amount = 14.99m }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify the payment now reflects the updated amount
        var getResponse = await CreateApiClient().GetAsync($"/api/payments/{created.Id}", ct);
        var body = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.CurrentAmount.ShouldBe(14.99m);
    }
}
