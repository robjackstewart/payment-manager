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

internal sealed class GetPayeeTests
{
    [Test]
    public async Task Handler_Handle_Should_ReturnPayee_When_PayeeWithMatchingIdExistsInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var matchingPayee = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Test Payee"
        };
        var nonMatchingPayee = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Other Payee"
        };
        var payees = new[] { matchingPayee, nonMatchingPayee };
        var payeesDbSet = payees.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(payeesDbSet);
        var logger = new FakeLogger<GetPayee.Handler>();
        var request = new GetPayee(matchingPayee.Id);
        var handler = new GetPayee.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(matchingPayee.Id);
        result.UserId.ShouldBe(matchingPayee.UserId);
        result.Name.ShouldBe(matchingPayee.Name);
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PayeeWithMatchingIdDoesNotExistInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var nonMatchingPayee = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Other Payee"
        };
        var payees = new[] { nonMatchingPayee };
        var payeesDbSet = payees.BuildMockDbSet();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(payeesDbSet);
        var logger = new FakeLogger<GetPayee.Handler>();
        var request = new GetPayee(Guid.NewGuid());
        var handler = new GetPayee.Handler(context, logger);
        var handle = new Func<Task>(() => handler.Handle(request, cancellationToken));

        // Act & Assert
        await handle.ShouldThrowAsync<NotFoundException<Payee>>();
    }
}
