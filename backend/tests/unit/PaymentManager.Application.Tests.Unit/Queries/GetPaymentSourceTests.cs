using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using Shouldly;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetPaymentSourceTests
{
    [Test]
    public async Task Handler_Handle_Should_Return_PaymentSourceWithMatchingId_When_PaymentSourceExists()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var matchingPaymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Test source"
        };
        var nonMatchingPaymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Other source"
        };
        var paymentSources = new[] { matchingPaymentSource, nonMatchingPaymentSource };
        var paymentSourcesDbSet = paymentSources.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.PaymentSources).Returns(paymentSourcesDbSet);
        var logger = new FakeLogger<GetPaymentSource.Handler>();
        var request = new GetPaymentSource(matchingPaymentSource.Id);
        var handler = new GetPaymentSource.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(matchingPaymentSource.Id);
        result.UserId.ShouldBe(matchingPaymentSource.UserId);
        result.Name.ShouldBe(matchingPaymentSource.Name);
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PaymentSourceDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var nonMatchingPaymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Other source"
        };
        var paymentSources = new[] { nonMatchingPaymentSource };
        var paymentSourcesDbSet = paymentSources.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.PaymentSources).Returns(paymentSourcesDbSet);
        var logger = new FakeLogger<GetPaymentSource.Handler>();
        var request = new GetPaymentSource(Guid.NewGuid());
        var handler = new GetPaymentSource.Handler(context, logger);
        var handle = new Func<Task>(() => handler.Handle(request, cancellationToken));

        // Act & Assert
        await handle.ShouldThrowAsync<NotFoundException<PaymentSource>>();
    }
}
