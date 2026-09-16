using NUnit.Framework;
using PaymentManager.Application.Common;
using Shouldly;

namespace PaymentManager.Application.Tests.Unit.Common;

internal sealed class SplitPaymentCalculatorTests
{
    // ── CalculateValue ────────────────────────────────────────────────────────

    [Test]
    public void CalculateValue_Should_ReturnCorrectAmount()
    {
        SplitPaymentCalculator.CalculateValue(200m, 25m).ShouldBe(50m);
    }

    [Test]
    public void CalculateValue_Should_TruncateToTwoDecimalPlaces()
    {
        // 100 * 33.333 / 100 = 33.333 → truncates to 33.33
        SplitPaymentCalculator.CalculateValue(100m, 33.333m).ShouldBe(33.33m);
    }

    [Test]
    public void CalculateValue_Should_ReturnFullAmount_When_PercentageIs100()
    {
        SplitPaymentCalculator.CalculateValue(150m, 100m).ShouldBe(150m);
    }

    [Test]
    public void CalculateValue_Should_ReturnZero_When_PercentageIsZero()
    {
        SplitPaymentCalculator.CalculateValue(500m, 0m).ShouldBe(0m);
    }

    // ── AllocateValues ────────────────────────────────────────────────────────

    [Test]
    public void AllocateValues_Should_ReturnEmpty_When_NoSplits()
    {
        SplitPaymentCalculator.AllocateValues(100m, []).ShouldBeEmpty();
    }

    [Test]
    public void AllocateValues_Should_SplitEvenly_When_NoRoundingNeeded()
    {
        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();
        var result = SplitPaymentCalculator.AllocateValues(200m, [(personA, 50m), (personB, 50m)]);

        result.Single(r => r.PersonId == personA).Value.ShouldBe(100m);
        result.Single(r => r.PersonId == personB).Value.ShouldBe(100m);
    }

    [Test]
    public void AllocateValues_Should_GiveLeftoverPennies_To_LargestSplit()
    {
        // 100 split three ways at 33.33/33.33/33.34 truncates to 33.33/33.33/33.34 = 99.99+? work it through:
        // floor(100*33.33/100*100)/100 = 33.33 for each of the first two; floor(100*33.34/100*100)/100 = 33.34.
        // Sum = 33.33 + 33.33 + 33.34 = 100.00 exactly, so use a case that actually leaves a remainder.
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        // 10 split three ways evenly (33.33/33.33/33.34) — each floor(10*33.33/100*100)/100 = 3.33 for the
        // first two and 3.33 for the third (floor(10*33.34/100*100)/100 = 3.334 -> 3.33), leftover = 0.01.
        var result = SplitPaymentCalculator.AllocateValues(10m, [(a, 33.33m), (b, 33.33m), (c, 33.34m)]);

        result.Sum(r => r.Value).ShouldBe(10m);
        // The leftover penny goes to whichever split has the largest percentage — c (33.34).
        result.Single(r => r.PersonId == c).Value.ShouldBe(result.Single(r => r.PersonId == a).Value + 0.01m);
    }

    [Test]
    public void AllocateValues_Should_GiveFullRemainder_To_SoleSplit()
    {
        var person = Guid.NewGuid();
        var result = SplitPaymentCalculator.AllocateValues(15.99m, [(person, 50m)]);

        // A single split with no one else to absorb the remainder takes all of it.
        result.Single().Value.ShouldBe(15.99m);
    }

    [Test]
    public void AllocateValues_Should_SumToExactAmount_ForFullySpecifiedSplits()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var result = SplitPaymentCalculator.AllocateValues(15.99m, [(a, 50m), (b, 50m)]);

        result.Sum(r => r.Value).ShouldBe(15.99m);
    }
}
