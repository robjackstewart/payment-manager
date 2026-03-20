using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using Shouldly;
using static PaymentManager.Application.Queries.GetAllPayees.Response;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetAllPayeesTests
{
    [Test]
    public async Task Handler_Handle_Should_ReturnOnlyMatchingPayees_OrderedById()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var payees = new[]
        {
            new Payee { Id = Guid.NewGuid(), UserId = targetUserId, Name = "Payee A" },
            new Payee { Id = Guid.NewGuid(), UserId = otherUserId, Name = "Payee B" },
            new Payee { Id = Guid.NewGuid(), UserId = targetUserId, Name = "Payee C" }
        };
        var payeesDbSet = payees.BuildMockDbSet();
        var expectedPayees = payees
            .Where(p => p.UserId == targetUserId)
            .OrderBy(p => p.Id)
            .Select(p => new PayeeDto(p.Id, p.UserId, p.Name))
            .ToArray();
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(payeesDbSet);
        var logger = new FakeLogger<GetAllPayees.Handler>();
        var request = new GetAllPayees(targetUserId);
        var handler = new GetAllPayees.Handler(context, logger);

        // Act
        var result = await handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Payees.Count.ShouldBe(2);
        result.Payees.ShouldBe(expectedPayees);
    }
}
