using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetPaymentOccurrencesTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PaymentSourceId = Guid.NewGuid();
    private static readonly Guid PayeeId = Guid.NewGuid();

    private static Payment MakePayment(PaymentFrequency frequency, DateOnly startDate, DateOnly? endDate = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = PaymentSourceId,
            PayeeId = PayeeId,
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = frequency,
            StartDate = startDate,
            EndDate = endDate
        };

    private static async Task<GetPaymentOccurrences.Response> Handle(
        Payment[] payments, DateOnly from, DateOnly to,
        CancellationToken ct = default) =>
        await Handle(payments, [], from, to, ct);

    private static async Task<GetPaymentOccurrences.Response> Handle(
        Payment[] payments, PaymentSplit[] splits, DateOnly from, DateOnly to,
        CancellationToken ct = default) =>
        await Handle(payments, splits, [], from, to, ct);

    private static async Task<GetPaymentOccurrences.Response> Handle(
        Payment[] payments, PaymentSplit[] splits, EffectivePaymentValue[] effectiveValues, DateOnly from, DateOnly to,
        CancellationToken ct = default)
    {
        var dbSet = payments.BuildMockDbSet();
        var splitsDbSet = splits.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(dbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.EffectivePaymentValues).Returns(effectiveValues.BuildMockDbSet());
        var logger = new FakeLogger<GetPaymentOccurrences.Handler>();
        return await new GetPaymentOccurrences.Handler(context, logger)
            .Handle(new GetPaymentOccurrences(UserId, from, to), ct);
    }

    // ── No payments ──────────────────────────────────────────────────────────

    [Test]
    public async Task NoPayments_Returns_EmptyOccurrences()
    {
        var result = await Handle([], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result.Occurrences.ShouldBeEmpty();
        result.Summary.ShouldBeEmpty();
    }

    [Test]
    public async Task PaymentWithNoEffectiveValues_UsesInitialAmount()
    {
        var payment = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 15));
        // Pass empty effectiveValues — should fall back to InitialAmount (100m)
        var result = await Handle([payment], [], [], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result.Occurrences.Count.ShouldBe(1);
        result.Occurrences.First().Amount.ShouldBe(100m);
    }

    // ── Once ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Once_StartDateWithinRange_Returns_OneOccurrence()
    {
        var payment = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 15));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result.Occurrences.Count.ShouldBe(1);
        result.Occurrences.First().OccurrenceDate.ShouldBe(new DateOnly(2025, 1, 15));
    }

    [Test]
    public async Task Once_StartDateBeforeRange_Returns_NoOccurrences()
    {
        var payment = MakePayment(PaymentFrequency.Once, new DateOnly(2024, 12, 31));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result.Occurrences.ShouldBeEmpty();
    }

    [Test]
    public async Task Once_StartDateAfterRange_Returns_NoOccurrences()
    {
        var payment = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 2, 1));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result.Occurrences.ShouldBeEmpty();
    }

    [Test]
    public async Task Once_StartDateOnRangeBoundaries_Returns_OneOccurrence_Each()
    {
        var p1 = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 1));
        var result1 = await Handle([p1], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result1.Occurrences.Count.ShouldBe(1);

        var p2 = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 31));
        var result2 = await Handle([p2], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result2.Occurrences.Count.ShouldBe(1);
    }

    // ── Monthly ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Monthly_SpanningThreeMonths_Returns_ThreeOccurrences()
    {
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 15));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));
        result.Occurrences.Count.ShouldBe(3);
        var dates = result.Occurrences.Select(o => o.OccurrenceDate).ToArray();
        dates.ShouldContain(new DateOnly(2025, 1, 15));
        dates.ShouldContain(new DateOnly(2025, 2, 15));
        dates.ShouldContain(new DateOnly(2025, 3, 15));
    }

    [Test]
    public async Task Monthly_EndDateCutsOff_Returns_CorrectCount()
    {
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 15), new DateOnly(2025, 2, 28));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));
        result.Occurrences.Count.ShouldBe(2);
    }

    [Test]
    public async Task Monthly_StartDateAfterRangeStart_OccurrencesStartFromStartDate()
    {
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 2, 10));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));
        result.Occurrences.Count.ShouldBe(2);
        var dates = result.Occurrences.Select(o => o.OccurrenceDate).ToArray();
        dates.ShouldContain(new DateOnly(2025, 2, 10));
        dates.ShouldContain(new DateOnly(2025, 3, 10));
    }

    [Test]
    public async Task Monthly_DayClampedToMonthEnd_ForFebruary()
    {
        // Day 31 should clamp to Feb 28 in non-leap year
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 31));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));
        result.Occurrences.Count.ShouldBe(3);
        var dates = result.Occurrences.Select(o => o.OccurrenceDate).ToArray();
        dates.ShouldContain(new DateOnly(2025, 1, 31));
        dates.ShouldContain(new DateOnly(2025, 2, 28));
        dates.ShouldContain(new DateOnly(2025, 3, 31));
    }

    [Test]
    public async Task Monthly_DayClampedToMonthEnd_ForFebruary_LeapYear()
    {
        // Day 31 should clamp to Feb 29 in leap year 2024
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2024, 1, 31));
        var result = await Handle([payment], new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31));
        result.Occurrences.Count.ShouldBe(3);
        var dates = result.Occurrences.Select(o => o.OccurrenceDate).ToArray();
        dates.ShouldContain(new DateOnly(2024, 1, 31));
        dates.ShouldContain(new DateOnly(2024, 2, 29));
        dates.ShouldContain(new DateOnly(2024, 3, 31));
    }

    // ── Annually ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Annually_AnniversaryInRange_Returns_OneOccurrence()
    {
        var payment = MakePayment(PaymentFrequency.Annually, new DateOnly(2023, 6, 15));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        result.Occurrences.Count.ShouldBe(1);
        result.Occurrences.First().OccurrenceDate.ShouldBe(new DateOnly(2025, 6, 15));
    }

    [Test]
    public async Task Annually_SpanningTwoYears_Returns_TwoOccurrences()
    {
        var payment = MakePayment(PaymentFrequency.Annually, new DateOnly(2023, 6, 15));
        var result = await Handle([payment], new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31));
        result.Occurrences.Count.ShouldBe(2);
    }

    [Test]
    public async Task Annually_AnniversaryOutsideRange_Returns_NoOccurrences()
    {
        var payment = MakePayment(PaymentFrequency.Annually, new DateOnly(2023, 8, 1));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 6, 30));
        result.Occurrences.ShouldBeEmpty();
    }

    [Test]
    public async Task Annually_EndDateExcludesAnniversary_Returns_NoOccurrence()
    {
        var payment = MakePayment(PaymentFrequency.Annually, new DateOnly(2023, 6, 15), new DateOnly(2024, 6, 14));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        result.Occurrences.ShouldBeEmpty();
    }

    [Test]
    public async Task Annually_Feb29_OnNonLeapYear_ClampsToFeb28()
    {
        var payment = MakePayment(PaymentFrequency.Annually, new DateOnly(2024, 2, 29));
        var result = await Handle([payment], new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        result.Occurrences.Count.ShouldBe(1);
        result.Occurrences.First().OccurrenceDate.ShouldBe(new DateOnly(2025, 2, 28));
    }

    // ── Ordering ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Occurrences_Are_OrderedByDate()
    {
        var p1 = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 20));
        var p2 = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 5));
        var result = await Handle([p1, p2], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result.Occurrences.Count.ShouldBe(2);
        result.Occurrences.Select(o => o.OccurrenceDate).ToArray()
            .ShouldBe([new DateOnly(2025, 1, 5), new DateOnly(2025, 1, 20)]);
    }

    // ── Other-user isolation ─────────────────────────────────────────────────

    [Test]
    public async Task OtherUserPayments_AreNotReturned()
    {
        var ownPayment = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 15));
        var otherPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(), // different user
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 1, 10),
        };
        var result = await Handle([ownPayment, otherPayment], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
        result.Occurrences.Count.ShouldBe(1);
        result.Occurrences.First().PaymentId.ShouldBe(ownPayment.Id);
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Summary_Should_Aggregate_Totals_By_Currency_And_PaymentSource()
    {
        // payment1: $100 from source1 with 40% split to contactId
        // payment2: $50  from source2 with no split
        var paymentSourceId1 = Guid.NewGuid();
        var paymentSourceId2 = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = paymentSourceId1,
            PayeeId = PayeeId,
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 1, 15)
        };
        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = paymentSourceId2,
            PayeeId = PayeeId,
            InitialAmount = 50m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            StartDate = new DateOnly(2025, 1, 20)
        };
        var split = new PaymentSplit { PaymentId = payment1.Id, ContactId = contactId, Percentage = 40m };
        // contact value = floor(100 * 40 / 100) = 40.00; user value = 100 - 40 = 60.00
        var effectiveValues = new[]
        {
            new EffectivePaymentValue { PaymentId = payment1.Id, EffectiveDate = payment1.StartDate, Amount = 100m },
            new EffectivePaymentValue { PaymentId = payment2.Id, EffectiveDate = payment2.StartDate, Amount = 50m },
        };

        var result = await Handle(
            [payment1, payment2], [split], effectiveValues,
            new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        result.Summary.Count.ShouldBe(1); // single currency group (USD)
        var usd = result.Summary.Single(s => s.Currency == "USD");
        usd.TotalAmount.ShouldBe(150m);   // 100 + 50
        usd.UserTotal.ShouldBe(110m);     // 60 + 50
        usd.ContactTotals.Count.ShouldBe(1);
        usd.ContactTotals.Single(c => c.ContactId == contactId).Amount.ShouldBe(40m);

        usd.ByPaymentSource.Count.ShouldBe(2);
        var ps1 = usd.ByPaymentSource.Single(ps => ps.PaymentSourceId == paymentSourceId1);
        ps1.TotalAmount.ShouldBe(100m);
        ps1.UserTotal.ShouldBe(60m);
        ps1.ContactTotals.Single(c => c.ContactId == contactId).Amount.ShouldBe(40m);

        var ps2 = usd.ByPaymentSource.Single(ps => ps.PaymentSourceId == paymentSourceId2);
        ps2.TotalAmount.ShouldBe(50m);
        ps2.UserTotal.ShouldBe(50m);
        ps2.ContactTotals.ShouldBeEmpty();
    }

    // ── Effective amount changes ──────────────────────────────────────────────

    [Test]
    public async Task EffectiveAmount_Changes_MidPeriod_OccurrenceUsesCorrectAmount()
    {
        // Monthly payment from 2025-01-01 with two effective values:
        // - from 2025-01-01: $10
        // - from 2025-03-01: $20
        // Jan and Feb occurrences use $10; March uses $20.
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 1));
        var effectiveValues = new[]
        {
            new EffectivePaymentValue { PaymentId = payment.Id, EffectiveDate = new DateOnly(2025, 1, 1), Amount = 10m },
            new EffectivePaymentValue { PaymentId = payment.Id, EffectiveDate = new DateOnly(2025, 3, 1), Amount = 20m },
        };

        var result = await Handle([payment], [], effectiveValues, new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));

        result.Occurrences.Count.ShouldBe(3);
        var ordered = result.Occurrences.OrderBy(o => o.OccurrenceDate).ToArray();
        ordered[0].Amount.ShouldBe(10m);   // Jan 1
        ordered[1].Amount.ShouldBe(10m);   // Feb 1
        ordered[2].Amount.ShouldBe(20m);   // Mar 1
    }

    [Test]
    public async Task EffectiveValue_AfterQueryRange_AllOccurrencesUseInitialAmount()
    {
        // Monthly payment with InitialAmount=100m.
        // EPV is effective 2026-01-01 — entirely after the query range (2025-01-01 → 2025-03-31).
        // Every occurrence in the range should use InitialAmount, not the EPV amount.
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 1));
        var effectiveValues = new[]
        {
            new EffectivePaymentValue { PaymentId = payment.Id, EffectiveDate = new DateOnly(2026, 1, 1), Amount = 200m },
        };

        var result = await Handle([payment], [], effectiveValues, new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));

        result.Occurrences.Count.ShouldBe(3);
        result.Occurrences.ShouldAllBe(o => o.Amount == 100m);  // InitialAmount, not 200m
    }
}
