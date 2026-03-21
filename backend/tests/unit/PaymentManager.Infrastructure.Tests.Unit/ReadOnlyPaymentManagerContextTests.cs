using FakeItEasy;
using MockQueryable.FakeItEasy;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;

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

    [Test]
    public void PaymentSources_Should_Return_ContextPaymentSources()
    {
        // Arrange
        var paymentSource = new PaymentSource
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = TestContext.CurrentContext.Random.GetString()
        };
        var paymentSources = new[] { paymentSource };
        var paymentSourcesDbSet = paymentSources.BuildMockDbSet();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PaymentSources).Returns(paymentSourcesDbSet);
        var readOnlyPaymentManagerContext = new ReadOnlyPaymentManagerContext(context);

        // Act
        var result = readOnlyPaymentManagerContext.PaymentSources.ToArray();

        // Assert
        result.ShouldBe(paymentSources);
    }

    [Test]
    public void Payees_Should_Return_ContextPayees()
    {
        // Arrange
        var payee = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = TestContext.CurrentContext.Random.GetString()
        };
        var payees = new[] { payee };
        var payeesDbSet = payees.BuildMockDbSet();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(payeesDbSet);
        var readOnlyPaymentManagerContext = new ReadOnlyPaymentManagerContext(context);

        // Act
        var result = readOnlyPaymentManagerContext.Payees.ToArray();

        // Assert
        result.ShouldBe(payees);
    }

    [Test]
    public void Payments_Should_Return_ContextPayments()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            Amount = 100.00m,
            Frequency = PaymentFrequency.Monthly,
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };
        var payments = new[] { payment };
        var paymentsDbSet = payments.BuildMockDbSet();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payments).Returns(paymentsDbSet);
        var readOnlyPaymentManagerContext = new ReadOnlyPaymentManagerContext(context);

        // Act
        var result = readOnlyPaymentManagerContext.Payments.ToArray();

        // Assert
        result.ShouldBe(payments);
    }
}
