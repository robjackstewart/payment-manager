using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using Shouldly;

namespace PaymentManager.Application.Tests.Unit.Common;

internal sealed class EffectiveSplitResolverTests
{
    private static readonly Guid PaymentId = Guid.NewGuid();

    private static EffectivePaymentSplit MakeSplit(DateOnly effectiveDate, Guid personId, decimal percentage) =>
        new()
        {
            PaymentId = PaymentId,
            EffectiveDate = effectiveDate,
            PersonId = personId,
            Percentage = percentage
        };

    // ── Resolve ───────────────────────────────────────────────────────────────

    [Test]
    public void Resolve_Should_ReturnInitialSplits_When_NoEffectiveSplits()
    {
        var personId = Guid.NewGuid();
        IReadOnlyList<(Guid PersonId, decimal Percentage)> initial = [(personId, 100m)];

        var result = EffectiveSplitResolver.Resolve(initial, [], new DateOnly(2025, 6, 1));

        result.ShouldBe(initial);
    }

    [Test]
    public void Resolve_Should_ReturnInitialSplits_When_AllEffectiveSplitsAreInTheFuture()
    {
        var initialPerson = Guid.NewGuid();
        var futurePerson = Guid.NewGuid();
        IReadOnlyList<(Guid PersonId, decimal Percentage)> initial = [(initialPerson, 100m)];
        EffectivePaymentSplit[] effectiveSplits =
        [
            MakeSplit(new DateOnly(2026, 1, 1), futurePerson, 100m)
        ];

        var result = EffectiveSplitResolver.Resolve(initial, effectiveSplits, new DateOnly(2025, 6, 1));

        result.ShouldBe(initial);
    }

    [Test]
    public void Resolve_Should_ReturnLatestSet_OnOrBeforeAsOfDate()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        IReadOnlyList<(Guid PersonId, decimal Percentage)> initial = [(alice, 100m)];
        EffectivePaymentSplit[] effectiveSplits =
        [
            MakeSplit(new DateOnly(2025, 3, 1), alice, 70m),
            MakeSplit(new DateOnly(2025, 3, 1), bob, 30m),
            MakeSplit(new DateOnly(2025, 9, 1), alice, 50m),
            MakeSplit(new DateOnly(2025, 9, 1), bob, 50m),
        ];

        var result = EffectiveSplitResolver.Resolve(initial, effectiveSplits, new DateOnly(2025, 6, 15));

        result.Count.ShouldBe(2);
        result.ShouldContain(x => x.PersonId == alice && x.Percentage == 70m);
        result.ShouldContain(x => x.PersonId == bob && x.Percentage == 30m);
    }

    [Test]
    public void Resolve_Should_ReturnWholeSet_ForDateEvenWhenMultiplePeopleShareIt()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var carol = Guid.NewGuid();
        EffectivePaymentSplit[] effectiveSplits =
        [
            MakeSplit(new DateOnly(2025, 1, 1), alice, 40m),
            MakeSplit(new DateOnly(2025, 1, 1), bob, 30m),
            MakeSplit(new DateOnly(2025, 1, 1), carol, 30m),
        ];

        var result = EffectiveSplitResolver.Resolve([], effectiveSplits, new DateOnly(2025, 5, 1));

        result.Count.ShouldBe(3);
        result.Sum(x => x.Percentage).ShouldBe(100m);
    }

    // ── GroupVersions ─────────────────────────────────────────────────────────

    [Test]
    public void GroupVersions_Should_GroupRowsByDateInAscendingOrder()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        EffectivePaymentSplit[] effectiveSplits =
        [
            MakeSplit(new DateOnly(2025, 3, 1), alice, 70m),
            MakeSplit(new DateOnly(2025, 3, 1), bob, 30m),
            MakeSplit(new DateOnly(2025, 9, 1), alice, 50m),
            MakeSplit(new DateOnly(2025, 9, 1), bob, 50m),
        ];

        var result = EffectiveSplitResolver.GroupVersions(effectiveSplits);

        result.Count.ShouldBe(2);
        result[0].EffectiveDate.ShouldBe(new DateOnly(2025, 3, 1));
        result[0].Splits.Count.ShouldBe(2);
        result[1].EffectiveDate.ShouldBe(new DateOnly(2025, 9, 1));
        result[1].Splits.Count.ShouldBe(2);
    }

    [Test]
    public void GroupVersions_Should_ReturnEmpty_When_NoEffectiveSplits() =>
        EffectiveSplitResolver.GroupVersions([]).ShouldBeEmpty();
}
