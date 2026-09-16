using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using PaymentManager.WebApi.Services;
using Shouldly;

namespace PaymentManager.WebApi.Tests.Integration.Endpoints;

internal sealed class PaymentOccurrenceTests : IntegrationTestBase
{
    private sealed record OccurrenceResponse(
        Guid PaymentId, Guid PaymentSourceId, Guid PayeeId,
        decimal Amount, string Currency, PaymentFrequency Frequency, PaymentDirection Direction,
        DateOnly OccurrenceDate, DateOnly StartDate, DateOnly? EndDate, string? Description, Guid? PayerGroupId,
        SplitDto[] Splits);

    private sealed record SplitDto(Guid PersonId, decimal Percentage, decimal Value);

    private sealed record GroupSummaryDto(Guid PayerGroupId, CurrencySummaryDto[] Currencies);

    private sealed record CurrencySummaryDto(
        string Currency, DirectionTotalsDto Outgoing, DirectionTotalsDto Incoming, NetTotalsDto Net,
        PayeeAmountDto[] OutgoingByPayee);

    private sealed record DirectionTotalsDto(
        decimal TotalAmount,
        PersonAmountDto[] PersonTotals, PaymentSourceBreakdownDto[] ByPaymentSource);

    private sealed record NetTotalsDto(
        decimal TotalAmount, PersonAmountDto[] PersonTotals);

    private sealed record PersonAmountDto(Guid PersonId, decimal Amount);

    private sealed record PayeeAmountDto(Guid PayeeId, decimal Amount);

    private sealed record PaymentSourceBreakdownDto(
        Guid PaymentSourceId, decimal TotalAmount,
        PersonAmountDto[] PersonTotals);

    private sealed record PersonCommitmentDto(
        Guid PersonId, string Currency, decimal Income, decimal Committed, decimal Remaining, bool IsOverCommitted);

    private sealed record GetOccurrencesResponse(OccurrenceResponse[] Occurrences, GroupSummaryDto[] Summary, PersonCommitmentDto[] People);

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

    private static void AddEffectiveValue(IPaymentManagerContext context, Guid paymentId, DateOnly effectiveDate, decimal amount)
    {
        context.EffectivePaymentValues.Add(new EffectivePaymentValue
        {
            PaymentId = paymentId,
            EffectiveDate = effectiveDate,
            Amount = amount
        });
    }

    private static Payment MakePayment(Guid userId, Guid psId, Guid payeeId, string currency, PaymentFrequency frequency, DateOnly startDate, DateOnly? endDate = null, PaymentDirection direction = PaymentDirection.Outgoing)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaymentSourceId = psId,
            PayeeId = payeeId,
            Currency = currency,
            Frequency = frequency,
            Direction = direction,
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
        body.People.ShouldBeEmpty();
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
            Direction = PaymentDirection.Outgoing,
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
    public async Task GetOccurrences_Should_Include_SplitValues_When_PaymentHasSplits()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var personId1 = await SetupPersonAsync("Alice", ct);
        var personId2 = await SetupPersonAsync("Bob", ct);
        var context = GetService<IPaymentManagerContext>();
        var payment = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 15));
        context.Payments.Add(payment);
        AddEffectiveValue(context, payment.Id, payment.StartDate, 100m);
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment.Id, PersonId = personId1, Percentage = 40m });
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment.Id, PersonId = personId2, Percentage = 60m });
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.Length.ShouldBe(1);
        var occurrence = body.Occurrences[0];
        occurrence.Splits.Length.ShouldBe(2);
        occurrence.Splits.Single(s => s.PersonId == personId1).Percentage.ShouldBe(40m);
        occurrence.Splits.Single(s => s.PersonId == personId1).Value.ShouldBe(40m);
        occurrence.Splits.Single(s => s.PersonId == personId2).Percentage.ShouldBe(60m);
        occurrence.Splits.Single(s => s.PersonId == personId2).Value.ShouldBe(60m);
    }

    [Test]
    public async Task GetOccurrences_Summary_Should_Aggregate_By_Currency_And_PaymentSource()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId1, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var paymentSource2 = new PaymentSource { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Mastercard" };
        var payerGroup = new PayerGroup { Id = Guid.NewGuid(), UserId = DefaultUserService.DefaultUserId, Name = "Family" };
        context.PaymentSources.Add(paymentSource2);
        context.PayerGroups.Add(payerGroup);
        await context.SaveChanges(ct);
        var psId2 = paymentSource2.Id;
        var personId1 = await SetupPersonAsync("Bob", ct);
        var personId2 = await SetupPersonAsync("Current User", ct);

        var payment1 = MakePayment(DefaultUserService.DefaultUserId, psId1, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 15)) with { PayerGroupId = payerGroup.Id };
        var payment2 = MakePayment(DefaultUserService.DefaultUserId, psId2, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 20)) with { PayerGroupId = payerGroup.Id };
        context.Payments.AddRange(payment1, payment2);
        AddEffectiveValue(context, payment1.Id, payment1.StartDate, 100m);
        AddEffectiveValue(context, payment2.Id, payment2.StartDate, 50m);
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment1.Id, PersonId = personId1, Percentage = 40m });
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = payment1.Id, PersonId = personId2, Percentage = 60m });
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Summary.Length.ShouldBe(1);
        body.Summary.Single().PayerGroupId.ShouldBe(payerGroup.Id);
        var usd = body.Summary.Single().Currencies.Single(c => c.Currency == "USD");
        usd.Outgoing.TotalAmount.ShouldBe(150m);     // 100 + 50
        usd.Outgoing.PersonTotals.Single(p => p.PersonId == personId1).Amount.ShouldBe(40m);
        usd.Outgoing.PersonTotals.Single(p => p.PersonId == personId2).Amount.ShouldBe(60m);

        var ps1 = usd.Outgoing.ByPaymentSource.Single(p => p.PaymentSourceId == psId1);
        ps1.TotalAmount.ShouldBe(100m);
        ps1.PersonTotals.Single(p => p.PersonId == personId1).Amount.ShouldBe(40m);
        ps1.PersonTotals.Single(p => p.PersonId == personId2).Amount.ShouldBe(60m);

        var ps2 = usd.Outgoing.ByPaymentSource.Single(p => p.PaymentSourceId == psId2);
        ps2.TotalAmount.ShouldBe(50m);
        ps2.PersonTotals.ShouldBeEmpty();

        usd.Net.TotalAmount.ShouldBe(-150m);
    }

    // ── Direction ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetOccurrences_IncomingPayment_Is_Counted_As_Income_Not_Outgoing()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        var (psId, payeeId) = await SetupPrerequisitesAsync(ct);
        var context = GetService<IPaymentManagerContext>();
        var personId = await SetupPersonAsync("Current User", ct);
        var income = MakePayment(DefaultUserService.DefaultUserId, psId, payeeId, "USD", PaymentFrequency.Once, new DateOnly(2025, 1, 15), direction: PaymentDirection.Incoming);
        context.Payments.Add(income);
        AddEffectiveValue(context, income.Id, income.StartDate, 3000m);
        context.PaymentSplits.Add(new PaymentSplit { PaymentId = income.Id, PersonId = personId, Percentage = 100m });
        await context.SaveChanges(ct);

        var response = await CreateApiClient()
            .GetAsync("/api/payments/occurrences?from=2025-01-01&to=2025-01-31", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOccurrencesResponse>(ct);
        body.ShouldNotBeNull();
        body.Occurrences.Single().Direction.ShouldBe(PaymentDirection.Incoming);
        var commitment = body.People.Single();
        commitment.Income.ShouldBe(3000m);
        commitment.Committed.ShouldBe(0m);
        // Income belongs to a person, never to a payer group.
        body.Summary.ShouldBeEmpty();
    }
}
