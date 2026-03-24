using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using Shouldly;
using static PaymentManager.Application.Queries.GetAllPaymentSources.Response;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetAllPaymentSourcesTests
{
    [Test]
    public async Task Handle_Should_Return_PaymentSourcesForMatchingUserId_OrderedById()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var paymentSources = new[]
        {
            new PaymentSource { Id = Guid.NewGuid(), UserId = targetUserId, Name = "Source 1" },
            new PaymentSource { Id = Guid.NewGuid(), UserId = targetUserId, Name = "Source 2" },
            new PaymentSource { Id = Guid.NewGuid(), UserId = otherUserId, Name = "Other source" }
        };
        var paymentSourcesDbSet = paymentSources.BuildMockDbSet();
        var expectedDtos = paymentSources
            .Where(ps => ps.UserId == targetUserId)
            .OrderBy(ps => ps.Id)
            .Select(ps => new PaymentSourceDto(ps.Id, ps.UserId, ps.Name))
            .ToArray();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.PaymentSources).Returns(paymentSourcesDbSet);
        var logger = new FakeLogger<GetAllPaymentSources.Handler>();
        var request = new GetAllPaymentSources(targetUserId);
        var handler = new GetAllPaymentSources.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.PaymentSources.Count.ShouldBe(2);
        result.PaymentSources.ShouldBe(expectedDtos);
    }
}
