using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using Shouldly;
using static PaymentManager.Application.Queries.GetAllPayments.Response;

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
                Amount = 100m,
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
                Amount = 50m,
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
                Amount = 200m,
                Currency = "USD",
                Frequency = PaymentFrequency.Annually,
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2026, 1, 1)
            }
        };
        var paymentsDbSet = payments.BuildMockDbSet();
        var splitsDbSet = Array.Empty<PaymentSplit>().BuildMockDbSet();
        var contactsDbSet = Array.Empty<Contact>().BuildMockDbSet();
        var expectedPayments = payments
            .Where(p => p.UserId == targetUserId)
            .OrderBy(p => p.Id)
            .Select(p => new PaymentDto(p.Id, p.UserId, p.PaymentSourceId, p.PayeeId, p.Amount, p.Currency, p.Frequency, p.StartDate, p.EndDate, p.Description, []))
            .ToArray();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        A.CallTo(() => context.PaymentSplits).Returns(splitsDbSet);
        A.CallTo(() => context.Contacts).Returns(contactsDbSet);
        var logger = new FakeLogger<GetAllPayments.Handler>();
        var request = new GetAllPayments(targetUserId);
        var handler = new GetAllPayments.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Payments.Count.ShouldBe(2);
        var resultIds = result.Payments.Select(p => p.Id).OrderBy(id => id).ToArray();
        var expectedIds = expectedPayments.Select(p => p.Id).OrderBy(id => id).ToArray();
        resultIds.ShouldBe(expectedIds);
        result.Payments.All(p => p.UserId == targetUserId).ShouldBeTrue();
        result.Payments.All(p => p.Splits.Count == 0).ShouldBeTrue();
    }
}
