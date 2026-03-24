using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
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

    // ── Combined usage ────────────────────────────────────────────────────────

    [Test]
    public void Combined_Should_ProduceConsistentUserShareAndSplitValues()
    {
        const decimal amount = 400m;
        decimal[] contactPercentages = [25m, 25m]; // total 50% to contacts

        var splitValues = contactPercentages.Select(p => SplitPaymentCalculator.CalculateValue(amount, p)).ToArray();
        var userSharePct = SplitPaymentCalculator.UserSharePercentage(contactPercentages);
        var userShareValue = SplitPaymentCalculator.UserShareValue(amount, splitValues);

        splitValues.ShouldBe([100m, 100m]);
        userSharePct.ShouldBe(50m);
        userShareValue.ShouldBe(200m);

        // All shares add up to the full amount
        (splitValues.Sum() + userShareValue).ShouldBe(amount);
    }

    // ── UserShareValue ────────────────────────────────────────────────────────

    [Test]
    public void UserShareValue_Should_Return_FullAmount_When_NoSplits()
    {
        SplitPaymentCalculator.UserShareValue(15.99m, []).ShouldBe(15.99m);
    }

    [Test]
    public void UserShareValue_Should_Return_Remainder_After_ContactSplits()
    {
        // 200 - 50 - 50 = 100
        SplitPaymentCalculator.UserShareValue(200m, [50m, 50m]).ShouldBe(100m);
    }

    [Test]
    public void UserShareValue_Should_Reconcile_When_Rounding_Would_Otherwise_Overflow()
    {
        // 15.99 / 50%: truncate(15.99 * 50 / 100) = truncate(7.995) = 7.99
        // Contact rounds DOWN; user absorbs the remainder (8.00), not the contact.
        var contactValue = SplitPaymentCalculator.CalculateValue(15.99m, 50m);
        var userValue = SplitPaymentCalculator.UserShareValue(15.99m, [contactValue]);

        contactValue.ShouldBe(7.99m);
        userValue.ShouldBe(8.00m);
        (contactValue + userValue).ShouldBe(15.99m);
    }
}
