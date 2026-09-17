using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
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
        Guid? PayerGroupId = null, IReadOnlyList<SplitRequest>? Splits = null,
        PaymentDirection Direction = PaymentDirection.Outgoing);

    private sealed record UpdateRequest(
        Guid PaymentSourceId, Guid PayeeId,
        decimal InitialAmount, string Currency, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate, string? Description = null,
        Guid? PayerGroupId = null, IReadOnlyList<SplitRequest>? Splits = null);

    private sealed record SplitRequest(Guid PersonId, decimal Percentage);

    private sealed record PaymentResponse(
        Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId,
        decimal CurrentAmount, decimal InitialAmount, string Currency, PaymentFrequency Frequency,
        DateOnly StartDate, DateOnly? EndDate, string? Description, Guid? PayerGroupId,
        SplitDto[] Splits, SplitDto[] InitialSplits, SplitVersionDto[] SplitVersions, ValueDto[] Values);

    private sealed record SplitDto(Guid PersonId, decimal Percentage, decimal Value);

    private sealed record SplitVersionDto(DateOnly EffectiveDate, SplitVersionSplitDto[] Splits);

    private sealed record SplitVersionSplitDto(Guid PersonId, decimal Percentage);

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

    private async Task<Guid> SetupPersonAsync(string name, CancellationToken ct)
    {
        var context = GetService<IPaymentManagerContext>();
        var person = new Person { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = name };
        context.People.Add(person);
        await context.SaveChanges(ct);
        return person.Id;
    }

    private async Task AddGroupMemberAsync(Guid payerGroupId, Guid personId, CancellationToken ct)
    {
        var context = GetService<IPaymentManagerContext>();
        context.PayerGroupMembers.Add(new PayerGroupMember { PayerGroupId = payerGroupId, PersonId = personId });
        await context.SaveChanges(ct);
    }

    private async Task<Guid> SetupPayerGroupAsync(string name, CancellationToken ct)
    {
        var context = GetService<IPaymentManagerContext>();
        var payerGroup = new PayerGroup { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = name };
        context.PayerGroups.Add(payerGroup);
        await context.SaveChanges(ct);
        return payerGroup.Id;
    }

    private static void AddEffectiveValue(IPaymentManagerContext context, Guid paymentId, DateOnly effectiveDate, decimal amount)
    {
        context.EffectivePaymentValues.Add(new EffectivePaymentValue
        {
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
        var ownerId = await SetupPersonAsync("Current User", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.UserId.ShouldBe(DefaultUserService.DefaultUserId);
        body.CurrentAmount.ShouldBe(9.99m);
        body.InitialAmount.ShouldBe(9.99m);
        body.Frequency.ShouldBe(PaymentFrequency.Monthly);
        body.Splits.Single().PersonId.ShouldBe(ownerId);
        body.Splits.Single().Value.ShouldBe(9.99m);
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

    [Test]
    public async Task CreatePayment_Should_Return_BadRequest_When_No_Splits_Provided()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
            Direction = PaymentDirection.Outgoing,
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
        body.InitialAmount.ShouldBe(15.99m);
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
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payments = new[]
        {
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 1, 1), InitialAmount = 10m },
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Annually, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 1, 1), InitialAmount = 10m },
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Once, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 6, 1), InitialAmount = 10m },
            new Payment { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, PaymentSourceId = paymentSourceId, PayeeId = payeeId, Currency = "USD", Frequency = PaymentFrequency.Monthly, Direction = PaymentDirection.Outgoing, StartDate = new DateOnly(2025, 3, 1), InitialAmount = 10m }
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
        var ownerId = await SetupPersonAsync("Current User", ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{payment.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 9.99m, "EUR", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.CurrentAmount.ShouldBe(9.99m);
        body.Currency.ShouldBe("EUR");
        body.Splits.Single().PersonId.ShouldBe(ownerId);
        body.Splits.Single().Value.ShouldBe(9.99m);
    }

    [Test]
    public async Task UpdatePayment_Should_Update_InitialAmount()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{payment.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 14.99m, "EUR", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.CurrentAmount.ShouldBe(14.99m);
        body.InitialAmount.ShouldBe(14.99m);
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
            Direction = PaymentDirection.Outgoing,
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
        var ownerId = await SetupPersonAsync("Current User", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, "My streaming service", Splits: [new SplitRequest(ownerId, 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Description.ShouldBe("My streaming service");
    }

    [Test]
    public async Task UpdatePayment_Should_Return_Updated_Description()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = paymentSourceId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            Description = "Original description",
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient().PutAsJsonAsync($"/api/payments/{payment.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, "Updated description", Splits: [new SplitRequest(ownerId, 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Description.ShouldBe("Updated description");
    }

    // ── Splits ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePayment_Should_Return_Splits_When_Provided()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var payerGroupId = await SetupPayerGroupAsync("Family", ct);
        var ownerId = await SetupPersonAsync("Current User", ct);
        var personId = await SetupPersonAsync("Derek", ct);
        await AddGroupMemberAsync(payerGroupId, ownerId, ct);
        await AddGroupMemberAsync(payerGroupId, personId, ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, payerGroupId,
            [new SplitRequest(personId, 30m), new SplitRequest(ownerId, 70m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Splits.Length.ShouldBe(2);
        body.Splits.Single(s => s.PersonId == personId).Percentage.ShouldBe(30m);
        body.Splits.Single(s => s.PersonId == personId).Value.ShouldBe(30m);
        body.Splits.Single(s => s.PersonId == ownerId).Percentage.ShouldBe(70m);
        body.Splits.Single(s => s.PersonId == ownerId).Value.ShouldBe(70m);
    }

    [Test]
    public async Task CreatePayment_Should_Return_BadRequest_When_Splits_Do_Not_Sum_To_100_Percent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var payerGroupId = await SetupPayerGroupAsync("Family", ct);
        var personId1 = await SetupPersonAsync("Alice", ct);
        var personId2 = await SetupPersonAsync("Bob", ct);
        await AddGroupMemberAsync(payerGroupId, personId1, ct);
        await AddGroupMemberAsync(payerGroupId, personId2, ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, payerGroupId,
            [new SplitRequest(personId1, 60m), new SplitRequest(personId2, 50m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreatePayment_Should_Return_NotFound_When_PersonId_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var payerGroupId = await SetupPayerGroupAsync("Family", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, payerGroupId,
            [new SplitRequest(Guid.NewGuid(), 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreatePayment_Should_Allow_Individual_Splits_Without_PayerGroup()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var personId = await SetupPersonAsync("Derek", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, null,
            [new SplitRequest(personId, 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.PayerGroupId.ShouldBeNull();
        body.Splits.Single().PersonId.ShouldBe(personId);
    }

    [Test]
    public async Task CreatePayment_Income_Should_Return_Created_With_Single_Person()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var personId = await SetupPersonAsync("Current User", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 3200m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, Splits: [new SplitRequest(personId, 100m)],
            Direction: PaymentDirection.Incoming), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.PayerGroupId.ShouldBeNull();
        body.Splits.Single().PersonId.ShouldBe(personId);
        body.Splits.Single().Percentage.ShouldBe(100m);
        body.Splits.Single().Value.ShouldBe(3200m);
    }

    [Test]
    public async Task CreatePayment_Income_Should_Return_BadRequest_When_Split_Across_Multiple_People()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var personId1 = await SetupPersonAsync("Current User", ct);
        var personId2 = await SetupPersonAsync("Jane", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 3200m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null,
            Splits: [new SplitRequest(personId1, 50m), new SplitRequest(personId2, 50m)],
            Direction: PaymentDirection.Incoming), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreatePayment_Income_Should_Return_BadRequest_When_PayerGroup_Is_Provided()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var payerGroupId = await SetupPayerGroupAsync("Family", ct);
        var personId = await SetupPersonAsync("Current User", ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 3200m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, PayerGroupId: payerGroupId,
            Splits: [new SplitRequest(personId, 100m)],
            Direction: PaymentDirection.Incoming), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreatePayment_Should_Return_BadRequest_When_SplitPerson_Is_Not_A_PayerGroupMember()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var payerGroupId = await SetupPayerGroupAsync("Family", ct);
        var otherGroupId = await SetupPayerGroupAsync("Housemates", ct);
        var personInOtherGroup = await SetupPersonAsync("Derek", ct);
        await AddGroupMemberAsync(otherGroupId, personInOtherGroup, ct);

        var response = await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, payerGroupId,
            [new SplitRequest(personInOtherGroup, 100m)]), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdatePayment_Should_Replace_Splits()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var payerGroupId = await SetupPayerGroupAsync("Family", ct);
        var ownerId = await SetupPersonAsync("Current User", ct);
        var personId1 = await SetupPersonAsync("Liz", ct);
        var personId2 = await SetupPersonAsync("Sam", ct);
        await AddGroupMemberAsync(payerGroupId, ownerId, ct);
        await AddGroupMemberAsync(payerGroupId, personId1, ct);
        await AddGroupMemberAsync(payerGroupId, personId2, ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, payerGroupId,
            [new SplitRequest(personId1, 25m), new SplitRequest(ownerId, 75m)]), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();
        created.Splits.Length.ShouldBe(2);

        var updateResponse = await CreateApiClient().PutAsJsonAsync($"/api/payments/{created.Id}", new UpdateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, payerGroupId,
            [new SplitRequest(personId2, 40m), new SplitRequest(ownerId, 60m)]), ct);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResponse.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Splits.Length.ShouldBe(2);
        body.Splits.Single(s => s.PersonId == personId2).Percentage.ShouldBe(40m);
        body.Splits.Single(s => s.PersonId == personId2).Value.ShouldBe(40m);
        body.Splits.Single(s => s.PersonId == ownerId).Percentage.ShouldBe(60m);
        body.Splits.Single(s => s.PersonId == ownerId).Value.ShouldBe(60m);
    }

    [Test]
    public async Task GetAllPayments_Should_Include_Splits()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var payerGroupId = await SetupPayerGroupAsync("Family", ct);
        var ownerId = await SetupPersonAsync("Current User", ct);
        var personId = await SetupPersonAsync("Frank", ct);
        await AddGroupMemberAsync(payerGroupId, ownerId, ct);
        await AddGroupMemberAsync(payerGroupId, personId, ct);
        await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 50m, "USD", PaymentFrequency.Once,
            new DateOnly(2026, 1, 1), null, null, payerGroupId,
            [new SplitRequest(personId, 50m), new SplitRequest(ownerId, 50m)]), ct);

        var response = await CreateApiClient().GetAsync("/api/payments", ct);
        var body = await response.Content.ReadFromJsonAsync<GetAllResponse>(ct);

        body.ShouldNotBeNull();
        body.Payments.Length.ShouldBe(1);
        body.Payments[0].Splits.Length.ShouldBe(2);
        body.Payments[0].Splits.Single(s => s.PersonId == personId).Percentage.ShouldBe(50m);
        body.Payments[0].Splits.Single(s => s.PersonId == personId).Value.ShouldBe(25m);
        body.Payments[0].Splits.Single(s => s.PersonId == ownerId).Value.ShouldBe(25m);
    }

    // ── AddPaymentValue ───────────────────────────────────────────────────────

    [Test]
    public async Task AddPaymentValue_Should_Return_Created_With_Value()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct))
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
        var ownerId = await SetupPersonAsync("Current User", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct))
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
        body.InitialAmount.ShouldBe(9.99m); // InitialAmount unchanged when adding EPV
    }

    [Test]
    public async Task AddPaymentValue_Should_Return_NotFound_When_Payment_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{Guid.NewGuid()}/values",
            new { effectiveDate = "2026-01-01", amount = 12.99m }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AddPaymentValue_Should_Return_BadRequest_When_EffectiveDate_Is_Before_StartDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 6, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        // Effective date is before the payment start date
        var response = await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/values",
            new { effectiveDate = "2025-01-01", amount = 12.99m }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AddPaymentValue_Should_Appear_In_Values_Array_When_Getting_Payment()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 9.99m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/values",
            new { effectiveDate = "2026-01-01", amount = 12.99m }, ct);

        var getResponse = await CreateApiClient().GetAsync($"/api/payments/{created.Id}", ct);
        var body = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.Values.Length.ShouldBe(1);
        body.Values[0].EffectiveDate.ShouldBe(new DateOnly(2026, 1, 1));
        body.Values[0].Amount.ShouldBe(12.99m);
    }

    // ── RemovePaymentValue ────────────────────────────────────────────────────

    [Test]
    public async Task RemovePaymentValue_Should_Return_NoContent()
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
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        AddEffectiveValue(context, payment.Id, new DateOnly(2026, 1, 1), 12.99m);
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .DeleteAsync($"/api/payments/{payment.Id}/values/2026-01-01", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Confirm the value is gone — current amount should revert to InitialAmount
        var getResponse = await CreateApiClient().GetAsync($"/api/payments/{payment.Id}", ct);
        var body = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.CurrentAmount.ShouldBe(9.99m);
    }

    [Test]
    public async Task RemovePaymentValue_Should_Return_NotFound_When_Payment_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient()
            .DeleteAsync($"/api/payments/{Guid.NewGuid()}/values/2026-01-01", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RemovePaymentValue_Should_Return_NotFound_When_Value_Does_Not_Exist()
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
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            InitialAmount = 9.99m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .DeleteAsync($"/api/payments/{payment.Id}/values/2026-01-01", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── AddPaymentSplits ──────────────────────────────────────────────────────

    [Test]
    public async Task AddPaymentSplits_Should_Return_Created_And_Expose_Version()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);
        var friendId = await SetupPersonAsync("Friend", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 50m), new SplitRequest(friendId, 50m)]), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/splits",
            new
            {
                effectiveDate = "2025-06-01",
                splits = new[]
                {
                    new { personId = ownerId, percentage = 70m },
                    new { personId = friendId, percentage = 30m }
                }
            }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var getResponse = await CreateApiClient().GetAsync($"/api/payments/{created.Id}", ct);
        var body = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.InitialSplits.Length.ShouldBe(2);
        body.InitialSplits.Single(s => s.PersonId == ownerId).Percentage.ShouldBe(50m);
        body.SplitVersions.Length.ShouldBe(1);
        body.SplitVersions[0].EffectiveDate.ShouldBe(new DateOnly(2025, 6, 1));
        body.SplitVersions[0].Splits.Single(s => s.PersonId == ownerId).Percentage.ShouldBe(70m);
        // The version took effect in the past, so it is the current split.
        body.Splits.Single(s => s.PersonId == ownerId).Percentage.ShouldBe(70m);
        body.Splits.Single(s => s.PersonId == ownerId).Value.ShouldBe(70m);
        body.Splits.Single(s => s.PersonId == friendId).Value.ShouldBe(30m);
    }

    [Test]
    public async Task AddPaymentSplits_Should_Return_BadRequest_When_Not_Totalling_100()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)]), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/splits",
            new { effectiveDate = "2025-06-01", splits = new[] { new { personId = ownerId, percentage = 40m } } }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        body.ShouldNotBeNull();
        body.Errors.ShouldContainKey("Splits");
    }

    [Test]
    public async Task AddPaymentSplits_Should_Return_BadRequest_For_Income_Payment()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 100m)], Direction: PaymentDirection.Incoming), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        var response = await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/splits",
            new { effectiveDate = "2025-06-01", splits = new[] { new { personId = ownerId, percentage = 100m } } }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── RemovePaymentSplits ───────────────────────────────────────────────────

    [Test]
    public async Task RemovePaymentSplits_Should_Return_NoContent()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (paymentSourceId, payeeId) = await SetupPrerequisitesAsync(ct);
        var ownerId = await SetupPersonAsync("Current User", ct);
        var friendId = await SetupPersonAsync("Friend", ct);

        var created = await (await CreateApiClient().PostAsJsonAsync("/api/payments", new CreateRequest(
            paymentSourceId, payeeId, 100m, "USD", PaymentFrequency.Monthly,
            new DateOnly(2025, 1, 1), null, Splits: [new SplitRequest(ownerId, 50m), new SplitRequest(friendId, 50m)]), ct))
            .Content.ReadFromJsonAsync<PaymentResponse>(ct);
        created.ShouldNotBeNull();

        await CreateApiClient().PostAsJsonAsync(
            $"/api/payments/{created.Id}/splits",
            new { effectiveDate = "2025-06-01", splits = new[] { new { personId = ownerId, percentage = 70m }, new { personId = friendId, percentage = 30m } } }, ct);

        var response = await CreateApiClient().DeleteAsync($"/api/payments/{created.Id}/splits/2025-06-01", ct);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await CreateApiClient().GetAsync($"/api/payments/{created.Id}", ct);
        var body = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(ct);
        body.ShouldNotBeNull();
        body.SplitVersions.ShouldBeEmpty();
        // Version removed, so the initial split applies again.
        body.Splits.Single(s => s.PersonId == ownerId).Percentage.ShouldBe(50m);
    }

    [Test]
    public async Task RemovePaymentSplits_Should_Return_NotFound_When_Payment_Does_Not_Exist()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient().DeleteAsync($"/api/payments/{Guid.NewGuid()}/splits/2025-06-01", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
