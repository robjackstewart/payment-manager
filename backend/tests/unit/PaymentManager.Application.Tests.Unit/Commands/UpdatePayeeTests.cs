using FakeItEasy;
using FluentValidation.TestHelper;
using MockQueryable.FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;
using PaymentManager.Application.Commands;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using Shouldly;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class UpdatePayeeTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForId_When_IdIsEmpty()
    {
        // Arrange
        var request = new UpdatePayee(Guid.Empty, Guid.NewGuid(), "Test Payee");
        var validator = new UpdatePayee.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_UserIdIsEmpty()
    {
        // Arrange
        var request = new UpdatePayee(Guid.NewGuid(), Guid.Empty, "Test Payee");
        var validator = new UpdatePayee.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_NameIsEmpty()
    {
        // Arrange
        var request = new UpdatePayee(Guid.NewGuid(), Guid.NewGuid(), string.Empty);
        var validator = new UpdatePayee.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        // Arrange
        var request = new UpdatePayee(Guid.NewGuid(), Guid.NewGuid(), "Test Payee");
        var validator = new UpdatePayee.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_UpdatePayeeInContext_When_PayeeExists()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var payeeId = Guid.NewGuid();
        var existingPayee = new Payee { Id = payeeId, UserId = Guid.NewGuid(), Name = "Old Name" };
        var dbSet = new[] { existingPayee }.BuildMockDbSet();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(dbSet);
        var logger = new FakeLogger<UpdatePayee.Handler>();
        var newUserId = Guid.NewGuid();
        var request = new UpdatePayee(payeeId, newUserId, "New Name");
        var handler = new UpdatePayee.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => dbSet.Update(A<Payee>._)).MustHaveHappenedOnceExactly();
        response.ShouldNotBeNull();
        response.Id.ShouldBe(payeeId);
        response.UserId.ShouldBe(newUserId);
        response.Name.ShouldBe("New Name");
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PayeeDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var dbSet = Array.Empty<Payee>().BuildMockDbSet();
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Payees).Returns(dbSet);
        var logger = new FakeLogger<UpdatePayee.Handler>();
        var request = new UpdatePayee(Guid.NewGuid(), Guid.NewGuid(), "Test Payee");
        var handler = new UpdatePayee.Handler(context, logger);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<Payee>>(() => handler.Handle(request, cancellationToken));
    }
}
