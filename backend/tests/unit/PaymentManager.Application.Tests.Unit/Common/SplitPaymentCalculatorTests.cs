using NUnit.Framework;
using PaymentManager.Application.Common;
using Shouldly;

namespace PaymentManager.Application.Tests.Unit.Common;

internal sealed class SplitPaymentCalculatorTests
{
    // ── UserSharePercentage ───────────────────────────────────────────────────

    [Test]
    public void UserSharePercentage_Should_Return100_When_NoSplits()
    {
        SplitPaymentCalculator.UserSharePercentage([]).ShouldBe(100m);
    }

    [Test]
    public void UserSharePercentage_Should_ReturnRemainder_When_SplitsPresent()
    {
        SplitPaymentCalculator.UserSharePercentage([30m, 20m]).ShouldBe(50m);
    }

    [Test]
    public void UserSharePercentage_Should_ReturnZero_When_SplitsTotalIs100()
    {
        SplitPaymentCalculator.UserSharePercentage([60m, 40m]).ShouldBe(0m);
    }

    // ── CalculateValue ────────────────────────────────────────────────────────

    [Test]
    public void CalculateValue_Should_ReturnCorrectAmount()
    {
        SplitPaymentCalculator.CalculateValue(200m, 25m).ShouldBe(50m);
    }

    [Test]
    public void CalculateValue_Should_RoundToTwoDecimalPlaces()
    {
        // 100 * 33.333 / 100 = 33.333 → rounds to 33.33
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

    // ── Combined usage ────────────────────────────────────────────────────────

    [Test]
    public void Combined_Should_ProduceConsistentUserShareAndSplitValues()
    {
        const decimal amount = 400m;
        decimal[] contactPercentages = [25m, 25m]; // total 50% to contacts

        var splitValues = contactPercentages.Select(p => SplitPaymentCalculator.CalculateValue(amount, p)).ToArray();
        var userSharePct = SplitPaymentCalculator.UserSharePercentage(contactPercentages);
        var userShareValue = SplitPaymentCalculator.CalculateValue(amount, userSharePct);

        splitValues.ShouldBe([100m, 100m]);
        userSharePct.ShouldBe(50m);
        userShareValue.ShouldBe(200m);

        // All shares add up to the full amount
        (splitValues.Sum() + userShareValue).ShouldBe(amount);
    }
}
