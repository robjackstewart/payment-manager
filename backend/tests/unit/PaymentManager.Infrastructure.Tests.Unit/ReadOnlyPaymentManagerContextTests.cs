using FakeItEasy;
using MockQueryable.FakeItEasy;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.Tests.Unit;

internal sealed class ReadOnlyPaymentManagerContextTests
{
    [Test]
    public void Users_Should_Return_ContextUsers()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = TestContext.CurrentContext.Random.GetString()
        };
        var users = new[] { user };
        var usersDbSet = users.BuildMockDbSet();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        var readOnlyPaymentManagerContext = new ReadOnlyPaymentManagerContext(context);

        // Act
        var result = readOnlyPaymentManagerContext.Users.ToArray();

        // Assert
        result.ShouldBe(users);
    }
}
