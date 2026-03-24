using FakeItEasy;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Testing;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using Shouldly;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class CreatePayeeTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_UserIdIsEmpty()
    {
        // Arrange
        var request = new CreatePayee(Guid.Empty, "Test Payee");
        var validator = new CreatePayee.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_NameIsEmpty()
    {
        // Arrange
        var request = new CreatePayee(Guid.NewGuid(), string.Empty);
        var validator = new CreatePayee.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        // Arrange
        var request = new CreatePayee(Guid.NewGuid(), "Test Payee");
        var validator = new CreatePayee.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_AddPayeeToContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var payees = Array.Empty<Payee>();
        var payeesDbSet = payees.BuildMockDbSet();
        A.CallTo(() => payeesDbSet.Add(A<Payee>._)).Invokes((Payee payee) => payees = payees.Append(payee).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(payeesDbSet);
        var logger = new FakeLogger<CreatePayee.Handler>();
        var userId = Guid.NewGuid();
        var request = new CreatePayee(userId, "Test Payee");
        var handler = new CreatePayee.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        payees.ShouldNotBeNull();
        payees.ShouldHaveSingleItem();
        payees.First().UserId.ShouldBe(userId);
        payees.First().Name.ShouldBe("Test Payee");
        response.ShouldNotBeNull();
        response.Id.ShouldNotBe(Guid.Empty);
        response.UserId.ShouldBe(userId);
        response.Name.ShouldBe("Test Payee");
    }
}
