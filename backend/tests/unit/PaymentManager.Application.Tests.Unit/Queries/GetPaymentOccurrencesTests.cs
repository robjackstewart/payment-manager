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

    private static Payment MakePayment(PaymentFrequency frequency, DateOnly startDate, DateOnly? endDate = null, PaymentDirection direction = PaymentDirection.Outgoing, Guid? payerGroupId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = PaymentSourceId,
            PayeeId = PayeeId,
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = frequency,
            Direction = direction,
            StartDate = startDate,
            EndDate = endDate,
            PayerGroupId = payerGroupId
        };

    /// <summary>Finds the currency summary for a given payer group. Ungrouped payments are not a
    /// group and appear only in <see cref="GetPaymentOccurrences.Response.People"/>.</summary>
    private static GetPaymentOccurrences.Response.CurrencySummaryDto GetCurrency(
        GetPaymentOccurrences.Response result, Guid payerGroupId, string currency) =>
        result.Summary.Single(g => g.PayerGroupId == payerGroupId).Currencies.Single(c => c.Currency == currency);

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
        CancellationToken ct = default) =>
        await Handle(payments, splits, effectiveValues, [], [], from, to, ct);

    private static async Task<GetPaymentOccurrences.Response> Handle(
        Payment[] payments, PaymentSplit[] splits, EffectivePaymentValue[] effectiveValues, PayerGroupMember[] members, DateOnly from, DateOnly to,
        CancellationToken ct = default) =>
        await Handle(payments, splits, effectiveValues, [], members, from, to, ct);

    private static async Task<GetPaymentOccurrences.Response> Handle(
        Payment[] payments, PaymentSplit[] splits, EffectivePaymentValue[] effectiveValues, EffectivePaymentSplit[] effectiveSplits, PayerGroupMember[] members, DateOnly from, DateOnly to,
        CancellationToken ct = default)
    {
        var dbSet = payments.BuildMockDbSet();
        var splitsDbSet = splits.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(dbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.EffectivePaymentSplits).Returns(effectiveSplits.BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentValues).Returns(effectiveValues.BuildMockDbSet());
        A.CallTo(() => context.PayerGroupMembers).Returns(members.BuildMockDbSet());
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
        result.People.ShouldBeEmpty();
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
            Direction = PaymentDirection.Outgoing,
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
        // payment1: $100 from source1 split 40% to personId, 60% to ownerId (splits must sum to 100)
        // payment2: $50  from source2 with no split
        var groupId = Guid.NewGuid();
        var paymentSourceId1 = Guid.NewGuid();
        var paymentSourceId2 = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = paymentSourceId1,
            PayeeId = PayeeId,
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 15),
            PayerGroupId = groupId
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
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 20),
            PayerGroupId = groupId
        };
        var splits = new[]
        {
            new PaymentSplit { PaymentId = payment1.Id, PersonId = personId, Percentage = 40m },
            new PaymentSplit { PaymentId = payment1.Id, PersonId = ownerId, Percentage = 60m },
        };
        var effectiveValues = new[]
        {
            new EffectivePaymentValue { PaymentId = payment1.Id, EffectiveDate = payment1.StartDate, Amount = 100m },
            new EffectivePaymentValue { PaymentId = payment2.Id, EffectiveDate = payment2.StartDate, Amount = 50m },
        };

        var result = await Handle(
            [payment1, payment2], splits, effectiveValues,
            new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        result.Summary.Count.ShouldBe(1); // a single real payer group
        result.Summary.Single().PayerGroupId.ShouldBe(groupId);
        var usd = GetCurrency(result, groupId, "USD");
        usd.Outgoing.TotalAmount.ShouldBe(150m);   // 100 + 50
        usd.Outgoing.PersonTotals.Count.ShouldBe(2);
        usd.Outgoing.PersonTotals.Single(p => p.PersonId == personId).Amount.ShouldBe(40m);
        usd.Outgoing.PersonTotals.Single(p => p.PersonId == ownerId).Amount.ShouldBe(60m);

        usd.Outgoing.ByPaymentSource.Count.ShouldBe(2);
        var ps1 = usd.Outgoing.ByPaymentSource.Single(ps => ps.PaymentSourceId == paymentSourceId1);
        ps1.TotalAmount.ShouldBe(100m);
        ps1.PersonTotals.Single(p => p.PersonId == personId).Amount.ShouldBe(40m);
        ps1.PersonTotals.Single(p => p.PersonId == ownerId).Amount.ShouldBe(60m);

        var ps2 = usd.Outgoing.ByPaymentSource.Single(ps => ps.PaymentSourceId == paymentSourceId2);
        ps2.TotalAmount.ShouldBe(50m);
        ps2.PersonTotals.ShouldBeEmpty();

        usd.Incoming.TotalAmount.ShouldBe(0m);
        usd.Net.TotalAmount.ShouldBe(-150m);
        usd.Net.PersonTotals.Single(p => p.PersonId == personId).Amount.ShouldBe(-40m);
        usd.Net.PersonTotals.Single(p => p.PersonId == ownerId).Amount.ShouldBe(-60m);

        // Both payments share the same PayeeId, so OutgoingByPayee has a single row.
        usd.OutgoingByPayee.Single(p => p.PayeeId == PayeeId).Amount.ShouldBe(150m);
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
    public async Task EffectiveSplitSet_Changes_MidPeriod_OccurrenceUsesCorrectSplit()
    {
        // Monthly payment from 2025-01-01 split 50/50 initially; from 2025-03-01 it becomes 70/30.
        // Jan/Feb occurrences use 50/50; March uses 70/30.
        var payment = MakePayment(PaymentFrequency.Monthly, new DateOnly(2025, 1, 1));
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var splits = new[]
        {
            new PaymentSplit { PaymentId = payment.Id, PersonId = alice, Percentage = 50m },
            new PaymentSplit { PaymentId = payment.Id, PersonId = bob, Percentage = 50m },
        };
        var effectiveSplits = new[]
        {
            new EffectivePaymentSplit { PaymentId = payment.Id, EffectiveDate = new DateOnly(2025, 3, 1), PersonId = alice, Percentage = 70m },
            new EffectivePaymentSplit { PaymentId = payment.Id, EffectiveDate = new DateOnly(2025, 3, 1), PersonId = bob, Percentage = 30m },
        };

        var result = await Handle(
            [payment], splits, [], effectiveSplits, [], new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));

        var ordered = result.Occurrences.OrderBy(o => o.OccurrenceDate).ToArray();
        ordered.Length.ShouldBe(3);
        ordered[0].Splits.Single(s => s.PersonId == alice).Percentage.ShouldBe(50m);
        ordered[0].Splits.Single(s => s.PersonId == alice).Value.ShouldBe(50m);
        ordered[1].Splits.Single(s => s.PersonId == alice).Percentage.ShouldBe(50m);
        ordered[2].Splits.Single(s => s.PersonId == alice).Percentage.ShouldBe(70m);
        ordered[2].Splits.Single(s => s.PersonId == alice).Value.ShouldBe(70m);
        ordered[2].Splits.Single(s => s.PersonId == bob).Percentage.ShouldBe(30m);
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

    // ── Direction / Net ──────────────────────────────────────────────────────

    [Test]
    public async Task IncomingPayment_Is_Excluded_From_Outgoing_And_Counted_As_Income()
    {
        var income = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 5), direction: PaymentDirection.Incoming);
        var personId = Guid.NewGuid();
        var split = new PaymentSplit { PaymentId = income.Id, PersonId = personId, Percentage = 100m };
        var result = await Handle([income], [split], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        result.Occurrences.Single().Direction.ShouldBe(PaymentDirection.Incoming);
        var commitment = result.People.Single();
        commitment.Income.ShouldBe(100m);
        commitment.Committed.ShouldBe(0m);
    }

    [Test]
    public async Task MonthMixingBothDirections_Net_Is_IncomingMinusOutgoing()
    {
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var income = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 5), direction: PaymentDirection.Incoming);
        var bill = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 15), direction: PaymentDirection.Outgoing, payerGroupId: groupId);
        var splits = new[]
        {
            new PaymentSplit { PaymentId = income.Id, PersonId = memberId, Percentage = 100m },
            new PaymentSplit { PaymentId = bill.Id, PersonId = memberId, Percentage = 100m },
        };
        var members = new[] { new PayerGroupMember { PayerGroupId = groupId, PersonId = memberId } };
        var result = await Handle([income, bill], splits, [], members, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        var usd = GetCurrency(result, groupId, "USD");
        usd.Incoming.TotalAmount.ShouldBe(100m);   // the member's income
        usd.Outgoing.TotalAmount.ShouldBe(100m);   // the grouped bill
        usd.Net.TotalAmount.ShouldBe(0m);
    }

    [Test]
    public async Task Person_PresentOnOnlyOneSide_StillAppears_InCommitments()
    {
        // Jane earns income (100% split to her, no bills) — her commitment shows income with no outgoings.
        var income = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 5), direction: PaymentDirection.Incoming);
        var personId = Guid.NewGuid();
        var split = new PaymentSplit { PaymentId = income.Id, PersonId = personId, Percentage = 100m };
        var result = await Handle([income], [split], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        var commitment = result.People.Single();
        commitment.Income.ShouldBe(100m);
        commitment.Committed.ShouldBe(0m);
        commitment.Remaining.ShouldBe(100m);
    }

    [Test]
    public async Task MultipleCurrencies_Are_Summarised_Independently()
    {
        var groupId = Guid.NewGuid();
        var usdBill = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 5), direction: PaymentDirection.Outgoing, payerGroupId: groupId);
        var gbpBill = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = PaymentSourceId,
            PayeeId = PayeeId,
            InitialAmount = 50m,
            Currency = "GBP",
            Frequency = PaymentFrequency.Once,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 10),
            PayerGroupId = groupId
        };
        var result = await Handle([usdBill, gbpBill], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        result.Summary.Count.ShouldBe(1);
        result.Summary.Single().Currencies.Count.ShouldBe(2);
        var usd = GetCurrency(result, groupId, "USD");
        var gbp = GetCurrency(result, groupId, "GBP");
        usd.Outgoing.TotalAmount.ShouldBe(100m);
        gbp.Outgoing.TotalAmount.ShouldBe(50m);
    }

    // ── Payer group dimension ────────────────────────────────────────────────

    [Test]
    public async Task PaymentsInDifferentPayerGroups_Produce_SeparateSummaryEntries()
    {
        var groupId1 = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();
        var grouped1 = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 10), direction: PaymentDirection.Outgoing, payerGroupId: groupId1);
        var grouped2 = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 15), direction: PaymentDirection.Outgoing, payerGroupId: groupId2);

        var result = await Handle([grouped1, grouped2], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        result.Summary.Count.ShouldBe(2);
        GetCurrency(result, groupId1, "USD").Outgoing.TotalAmount.ShouldBe(100m);
        GetCurrency(result, groupId2, "USD").Outgoing.TotalAmount.ShouldBe(100m);
    }

    [Test]
    public async Task UngroupedPayments_DoNotProduce_ASummaryEntry()
    {
        var personal = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 15), direction: PaymentDirection.Outgoing);

        var result = await Handle([personal], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        result.Summary.ShouldBeEmpty();
    }

    [Test]
    public async Task GroupIncoming_Is_SumOfMembersIncome_RegardlessOfWhereItLives()
    {
        // A group's income is never assigned to the group directly (income payments are always
        // ungrouped) — it is the sum of what each member earns, wherever that income lives.
        var groupId = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();
        var nonMember = Guid.NewGuid();

        var incomeA = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 5), direction: PaymentDirection.Incoming);
        var incomeB = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 6), direction: PaymentDirection.Incoming);
        var incomeOutsider = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 7), direction: PaymentDirection.Incoming);
        var groupBill = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 10), direction: PaymentDirection.Outgoing, payerGroupId: groupId);

        var splits = new[]
        {
            new PaymentSplit { PaymentId = incomeA.Id, PersonId = memberA, Percentage = 100m },
            new PaymentSplit { PaymentId = incomeB.Id, PersonId = memberB, Percentage = 100m },
            new PaymentSplit { PaymentId = incomeOutsider.Id, PersonId = nonMember, Percentage = 100m },
        };
        var members = new[]
        {
            new PayerGroupMember { PayerGroupId = groupId, PersonId = memberA },
            new PayerGroupMember { PayerGroupId = groupId, PersonId = memberB },
        };

        var result = await Handle(
            [incomeA, incomeB, incomeOutsider, groupBill], splits, [], members,
            new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        var usd = GetCurrency(result, groupId, "USD");
        usd.Incoming.TotalAmount.ShouldBe(200m); // memberA + memberB only, not the non-member
        usd.Incoming.PersonTotals.Count.ShouldBe(2);
        usd.Incoming.PersonTotals.Single(p => p.PersonId == memberA).Amount.ShouldBe(100m);
        usd.Incoming.PersonTotals.Single(p => p.PersonId == memberB).Amount.ShouldBe(100m);

        // The non-member's income is still tracked against them personally.
        result.People.Single(p => p.PersonId == memberA).Income.ShouldBe(100m);
        result.People.Single(p => p.PersonId == memberB).Income.ShouldBe(100m);
        result.People.Single(p => p.PersonId == nonMember).Income.ShouldBe(100m);
    }

    [Test]
    public async Task People_Should_FlagOverCommitment_AcrossAllGroupsAndPersonal()
    {
        // A person's committed outgoing share across every group (plus personal payments) can
        // exceed their income — that is what IsOverCommitted surfaces.
        var groupId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var income = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 5), direction: PaymentDirection.Incoming);
        var groupBill = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 10), direction: PaymentDirection.Outgoing, payerGroupId: groupId);
        var personalBill = MakePayment(PaymentFrequency.Once, new DateOnly(2025, 1, 15), direction: PaymentDirection.Outgoing);
        // personalBill needs a value; MakePayment defaults InitialAmount to 100m for all three.

        var splits = new[]
        {
            new PaymentSplit { PaymentId = income.Id, PersonId = personId, Percentage = 100m }, // income: 100
            new PaymentSplit { PaymentId = groupBill.Id, PersonId = personId, Percentage = 100m }, // committed: 100
            new PaymentSplit { PaymentId = personalBill.Id, PersonId = personId, Percentage = 100m }, // committed: 100
        };

        var result = await Handle([income, groupBill, personalBill], splits, [], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        var commitment = result.People.Single(p => p.PersonId == personId && p.Currency == "USD");
        commitment.Income.ShouldBe(100m);
        commitment.Committed.ShouldBe(200m);
        commitment.Remaining.ShouldBe(-100m);
        commitment.IsOverCommitted.ShouldBeTrue();
    }

    [Test]
    public async Task OutgoingByPayee_Aggregates_MultiplePayments_ToSamePayee()
    {
        var payeeId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var p1 = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = PaymentSourceId,
            PayeeId = payeeId,
            InitialAmount = 30m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 5),
            PayerGroupId = groupId
        };
        var p2 = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = PaymentSourceId,
            PayeeId = payeeId,
            InitialAmount = 20m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 20),
            PayerGroupId = groupId
        };
        // Income to a different payee must not leak into OutgoingByPayee.
        var income = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PaymentSourceId = PaymentSourceId,
            PayeeId = Guid.NewGuid(),
            InitialAmount = 500m,
            Currency = "USD",
            Frequency = PaymentFrequency.Once,
            Direction = PaymentDirection.Incoming,
            StartDate = new DateOnly(2025, 1, 10)
        };

        var result = await Handle([p1, p2, income], new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));

        var usd = GetCurrency(result, groupId, "USD");
        usd.OutgoingByPayee.Single().PayeeId.ShouldBe(payeeId);
        usd.OutgoingByPayee.Single().Amount.ShouldBe(50m);
    }
}
