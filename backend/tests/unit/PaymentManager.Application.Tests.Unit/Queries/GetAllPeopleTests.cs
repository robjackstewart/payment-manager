using FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Queries;
using PaymentManager.Domain.Entities;
using Shouldly;

namespace PaymentManager.Application.Tests.Unit.Queries;

internal sealed class GetAllPeopleTests
{
    [Test]
    public async Task Handler_Handle_Should_Return_OnlyPeopleForMatchingUserId()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var people = new[]
        {
            new Person { Id = Guid.NewGuid(), UserId = targetUserId, Name = "Alice" },
            new Person { Id = Guid.NewGuid(), UserId = otherUserId, Name = "Someone Else" }
        };
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(people.BuildMockDbSet());
        var logger = new FakeLogger<GetAllPeople.Handler>();
        var handler = new GetAllPeople.Handler(context, logger);

        var response = await handler.Handle(new GetAllPeople(targetUserId), cancellationToken);

        response.People.ShouldHaveSingleItem();
        response.People.Single().Name.ShouldBe("Alice");
    }

    [Test]
    public async Task Handler_Handle_Should_Order_Alphabetically()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var userId = Guid.NewGuid();
        var people = new[]
        {
            new Person { Id = Guid.NewGuid(), UserId = userId, Name = "Zara" },
            new Person { Id = Guid.NewGuid(), UserId = userId, Name = "Alice" },
            new Person { Id = Guid.NewGuid(), UserId = userId, Name = "Current User" }
        };
        var context = A.Fake<IReadOnlyPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(people.BuildMockDbSet());
        var logger = new FakeLogger<GetAllPeople.Handler>();
        var handler = new GetAllPeople.Handler(context, logger);

        var response = await handler.Handle(new GetAllPeople(userId), cancellationToken);

        response.People.Select(p => p.Name).ShouldBe(["Alice", "Current User", "Zara"]);
    }
}
