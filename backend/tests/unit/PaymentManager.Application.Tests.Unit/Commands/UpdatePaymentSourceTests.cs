using NUnit.Framework;
using PaymentManager.Application.Commands;
using FluentValidation.TestHelper;
using FakeItEasy;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using MockQueryable.FakeItEasy;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;
using static PaymentManager.Application.Common.Exceptions;
using PaymentManager.Application.Common;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class UpdatePaymentSourceTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForId_When_IdIsEmpty()
    {
        // Arrange
        var request = new UpdatePaymentSource(Guid.Empty, Guid.NewGuid(), "Test source");
        var validator = new UpdatePaymentSource.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_UserIdIsEmpty()
    {
        // Arrange
        var request = new UpdatePaymentSource(Guid.NewGuid(), Guid.Empty, "Test source");
        var validator = new UpdatePaymentSource.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_NameIsEmpty()
    {
        // Arrange
        var request = new UpdatePaymentSource(Guid.NewGuid(), Guid.NewGuid(), string.Empty);
        var validator = new UpdatePaymentSource.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        // Arrange
        var request = new UpdatePaymentSource(Guid.NewGuid(), Guid.NewGuid(), "Test source");
        var validator = new UpdatePaymentSource.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_UpdatePaymentSourceInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existingPaymentSource = new PaymentSource { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Old name" };
        var context = A.Fake<IPaymentManagerContext>();
        var dbSet = new[] { existingPaymentSource }.BuildMockDbSet();
        A.CallTo(() => context.PaymentSources).Returns(dbSet);
        var logger = new FakeLogger<UpdatePaymentSource.Handler>();
        var newUserId = Guid.NewGuid();
        var request = new UpdatePaymentSource(existingPaymentSource.Id, newUserId, "New name");
        var handler = new UpdatePaymentSource.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => dbSet.Update(A<PaymentSource>.That.Matches(ps => ps.Name == "New name" && ps.UserId == newUserId))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
        response.ShouldNotBeNull();
        response.Id.ShouldBe(existingPaymentSource.Id);
        response.UserId.ShouldBe(newUserId);
        response.Name.ShouldBe("New name");
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PaymentSourceDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var dbSet = Array.Empty<PaymentSource>().BuildMockDbSet();
        A.CallTo(() => context.PaymentSources).Returns(dbSet);
        var logger = new FakeLogger<UpdatePaymentSource.Handler>();
        var request = new UpdatePaymentSource(Guid.NewGuid(), Guid.NewGuid(), "Test source");
        var handler = new UpdatePaymentSource.Handler(context, logger);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<PaymentSource>>(() => handler.Handle(request, cancellationToken));
    }
}
