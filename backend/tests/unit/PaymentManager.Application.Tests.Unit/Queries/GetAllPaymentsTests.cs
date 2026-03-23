using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetAllPaymentsTests
{
    [Test]
    public async Task Handler_Handle_Should_Return_OnlyPaymentsForMatchingUserId()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var payments = new[]
        {
            new Payment
            {
                Id = Guid.NewGuid(),
                UserId = targetUserId,
                PaymentSourceId = Guid.NewGuid(),
                PayeeId = Guid.NewGuid(),
                InitialAmount = 100m,
                Currency = "USD",
                Frequency = PaymentFrequency.Monthly,
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 12, 31)
            },
            new Payment
            {
                Id = Guid.NewGuid(),
                UserId = targetUserId,
                PaymentSourceId = Guid.NewGuid(),
                PayeeId = Guid.NewGuid(),
                InitialAmount = 50m,
                Currency = "USD",
                Frequency = PaymentFrequency.Once,
                StartDate = new DateOnly(2025, 6, 1),
                EndDate = null
            },
            new Payment
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                PaymentSourceId = Guid.NewGuid(),
                PayeeId = Guid.NewGuid(),
                InitialAmount = 200m,
                Currency = "USD",
                Frequency = PaymentFrequency.Annually,
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2026, 1, 1)
            }
        };
        var expectedIds = payments
            .Where(p => p.UserId == targetUserId)
            .Select(p => p.Id)
            .OrderBy(id => id)
            .ToArray();
        var paymentsDbSet = payments.BuildMockDbSet();
        var splitsDbSet = Array.Empty<PaymentSplit>().BuildMockDbSet();
        var effectiveValues = new[]
        {
            new EffectivePaymentValue { Id = Guid.NewGuid(), PaymentId = payments[0].Id, EffectiveDate = payments[0].StartDate, Amount = 100m },
            new EffectivePaymentValue { Id = Guid.NewGuid(), PaymentId = payments[1].Id, EffectiveDate = payments[1].StartDate, Amount = 50m },
            new EffectivePaymentValue { Id = Guid.NewGuid(), PaymentId = payments[2].Id, EffectiveDate = payments[2].StartDate, Amount = 200m },
        };
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.EffectivePaymentValues).Returns(effectiveValues.BuildMockDbSet());
        var logger = new FakeLogger<GetAllPayments.Handler>();
        var request = new GetAllPayments(targetUserId);
        var handler = new GetAllPayments.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Payments.Count.ShouldBe(2);
        var resultIds = result.Payments.Select(p => p.Id).OrderBy(id => id).ToArray();
        resultIds.ShouldBe(expectedIds);
        result.Payments.All(p => p.UserId == targetUserId).ShouldBeTrue();
        result.Payments.All(p => p.Splits.Count == 0).ShouldBeTrue();
    }

    [Test]
    public async Task Handler_Handle_Should_Compute_UserShareAndSplitValues_For_PaymentWithSplits()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var contactId1 = Guid.NewGuid();
        var contactId2 = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 200m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2025, 1, 1),
        };
        var splits = new[]
        {
            new PaymentSplit { PaymentId = payment.Id, ContactId = contactId1, Percentage = 30m },
            new PaymentSplit { PaymentId = payment.Id, ContactId = contactId2, Percentage = 20m },
        };
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(new[] { payment }.BuildMockDbSet());
        A.CallTo(() => context.PaymentSplits).Returns(splits.BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentValues).Returns(new[]
        {
            new EffectivePaymentValue { Id = Guid.NewGuid(), PaymentId = payment.Id, EffectiveDate = new DateOnly(2025, 1, 1), Amount = 200m }
        }.BuildMockDbSet());
        var handler = new GetAllPayments.Handler(context, new FakeLogger<GetAllPayments.Handler>());

        // Act
        var result = await handler.Handle(new GetAllPayments(userId), cancellationToken);

        // Assert
        var dto = result.Payments.Single();
        dto.UserShare.Percentage.ShouldBe(50m);      // 100 - 30 - 20
        dto.UserShare.Value.ShouldBe(100m);           // 200 * 50 / 100
        dto.Splits.Count.ShouldBe(2);
        var alice = dto.Splits.Single(s => s.ContactId == contactId1);
        alice.Percentage.ShouldBe(30m);
        alice.Value.ShouldBe(60m);                   // 200 * 30 / 100
        var bob = dto.Splits.Single(s => s.ContactId == contactId2);
        bob.Percentage.ShouldBe(20m);
        bob.Value.ShouldBe(40m);                     // 200 * 20 / 100
    }
}
