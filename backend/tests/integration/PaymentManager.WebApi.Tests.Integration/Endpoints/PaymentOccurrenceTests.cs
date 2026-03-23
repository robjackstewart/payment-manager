using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using PaymentManager.WebApi.Services;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class PaymentOccurrenceTests : IntegrationTestBase
{
    private sealed record OccurrenceResponse(
        Guid PaymentId, Guid PaymentSourceId, Guid PayeeId,
        decimal Amount, string Currency, PaymentFrequency Frequency,
        DateOnly OccurrenceDate, DateOnly StartDate, DateOnly? EndDate,
        UserShareDto UserShare, SplitDto[] Splits);

    private sealed record SplitDto(Guid ContactId, decimal Percentage, decimal Value);

    private sealed record SummaryDto(
        string Currency, decimal TotalAmount, decimal UserTotal,
        ContactAmountDto[] ContactTotals, PaymentSourceBreakdownDto[] ByPaymentSource);

    private sealed record ContactAmountDto(Guid ContactId, decimal Amount);

    private sealed record PaymentSourceBreakdownDto(
        Guid PaymentSourceId, decimal TotalAmount, decimal UserTotal,
        ContactAmountDto[] ContactTotals);

    private sealed record GetOccurrencesResponse(OccurrenceResponse[] Occurrences, SummaryDto[] Summary);

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

    private static Payment MakePayment(Guid userId, Guid psId, Guid payeeId, string currency, PaymentFrequency frequency, DateOnly startDate, DateOnly? endDate = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaymentSourceId = psId,
            PayeeId = payeeId,
            Currency = currency,
            Frequency = frequency,
            StartDate = startDate,
            EndDate = endDate,
            InitialAmount = 100m
        };

    // ── Empty range ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetOccurrences_NoPayments_Returns_EmptyList()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.ShouldBeEmpty();
        body.Summary.ShouldBeEmpty();
    }

    // ── InitialAmount fallback ────────────────────────────────────────────────

    [Test]
    public async Task GetOccurrences_PaymentWithNoEffectiveValues_UsesInitialAmount()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserService.DefaultUserId,
            PaymentSourceId = psId,
            PayeeId = payeeId,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 1, 15),
            InitialAmount = 75m
        };
        context.Payments.Add(payment);
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.Length.ShouldBe(1);
        body.Occurrences[0].Amount.ShouldBe(75m);
        body.Occurrences[0].UserShare.Value.ShouldBe(75m);
    }

    // ── Once ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetOccurrences_OncePayment_InRange_Returns_OneOccurrence()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 15));
        context.Payments.Add(payment);
        AddEffectiveValue(context, payment.Id, payment.StartDate, 50m);
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.Length.ShouldBe(1);
        body.Occurrences[0].PaymentId.ShouldBe(payment.Id);
        body.Occurrences[0].OccurrenceDate.ShouldBe(new DateOnly(2025, 1, 15));
        body.Occurrences[0].Amount.ShouldBe(50m);
        body.Occurrences[0].UserShare.Percentage.ShouldBe(100m);
        body.Occurrences[0].UserShare.Value.ShouldBe(50m);
    }

    [Test]
    public async Task GetOccurrences_OncePayment_OutOfRange_Returns_NoOccurrences()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 3, 10));
        context.Payments.Add(payment);
        AddEffectiveValue(context, payment.Id, payment.StartDate, 50m);
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.ShouldBeEmpty();
    }

    // ── Monthly ───────────────────────────────────────────────────────────────

    [Test]
    public async Task GetOccurrences_MonthlyPayment_SpanningThreeMonths_Returns_ThreeOccurrences()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "GBP", PaymentFrequency.Monthly, new DateOnly(2025, 1, 10));
        context.Payments.Add(payment);
        AddEffectiveValue(context, payment.Id, payment.StartDate, 9.99m);
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-03-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.Length.ShouldBe(3);
        body.Occurrences.ShouldAllBe(o => o.PaymentId == payment.Id);
        body.Occurrences.ShouldAllBe(o => o.UserShare.Percentage == 100m);
        body.Occurrences.ShouldAllBe(o => o.UserShare.Value == 9.99m);
        var dates = body.Occurrences.Select(o => o.OccurrenceDate).ToArray();
        dates.ShouldContain(new DateOnly(2025, 1, 10));
        dates.ShouldContain(new DateOnly(2025, 2, 10));
        dates.ShouldContain(new DateOnly(2025, 3, 10));
    }

    // ── Occurrences ordered by date ──────────────────────────────────────────

    [Test]
    public async Task GetOccurrences_MultiplePayments_Returns_OccurrencesOrderedByDate()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var p1 = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 20));
        var p2 = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 5));
        context.Payments.AddRange(p1, p2);
        AddEffectiveValue(context, p1.Id, p1.StartDate, 20m);
        AddEffectiveValue(context, p2.Id, p2.StartDate, 10m);
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.Length.ShouldBe(2);
        body.Occurrences[0].OccurrenceDate.ShouldBe(new DateOnly(2025, 1, 5));
        body.Occurrences[1].OccurrenceDate.ShouldBe(new DateOnly(2025, 1, 20));
    }

    // ── Splits ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetOccurrences_Should_Include_UserShare_And_SplitValues_When_PaymentHasSplits()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var contactId = await SetupContactAsync("Alice", ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 15));
        context.Payments.Add(payment);
        AddEffectiveValue(context, payment.Id, payment.StartDate, 100m);
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment.Id, ContactId = contactId, Percentage = 40m });
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.Length.ShouldBe(1);
        var occurrence = body.Occurrences[0];
        occurrence.UserShare.Percentage.ShouldBe(60m);
        occurrence.UserShare.Value.ShouldBe(60m);
        occurrence.Splits.Length.ShouldBe(1);
        occurrence.Splits[0].ContactId.ShouldBe(contactId);
        occurrence.Splits[0].Percentage.ShouldBe(40m);
        occurrence.Splits[0].Value.ShouldBe(40m);
    }

    [Test]
    public async Task GetOccurrences_Summary_Should_Aggregate_By_Currency_And_PaymentSource()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId1, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var paymentSource2 = new PaymentSource { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Mastercard" };
        context.PaymentSources.Add(paymentSource2);
        await context.SaveChanges(ct);
        var psId2 = paymentSource2.Id;
        var contactId = await SetupContactAsync("Bob", ct);

        var payment1 = MakePayment(DefaultUserService.DefaultUserId, psId1, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 15));
        var payment2 = MakePayment(DefaultUserService.DefaultUserId, psId2, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 20));
        context.Payments.AddRange(payment1, payment2);
        AddEffectiveValue(context, payment1.Id, payment1.StartDate, 100m);
        AddEffectiveValue(context, payment2.Id, payment2.StartDate, 50m);
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment1.Id, ContactId = contactId, Percentage = 40m });
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Summary.Length.ShouldBe(1);
        var usd = body.Summary.Single(s => s.Currency == "USD");
        usd.TotalAmount.ShouldBe(150m);     // 100 + 50
        usd.UserTotal.ShouldBe(110m);        // 60 (user share of p1) + 50 (full p2)
        usd.ContactTotals.Single(c => c.ContactId == contactId).Amount.ShouldBe(40m);

        var ps1 = usd.ByPaymentSource.Single(p => p.PaymentSourceId == psId1);
        ps1.TotalAmount.ShouldBe(100m);
        ps1.UserTotal.ShouldBe(60m);
        ps1.ContactTotals.Single(c => c.ContactId == contactId).Amount.ShouldBe(40m);

        var ps2 = usd.ByPaymentSource.Single(p => p.PaymentSourceId == psId2);
        ps2.TotalAmount.ShouldBe(50m);
        ps2.UserTotal.ShouldBe(50m);
        ps2.ContactTotals.ShouldBeEmpty();
    }
}
