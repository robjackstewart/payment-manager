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

internal sealed class UpdateUserTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForId_When_IdIsEmpty()
    {
        // Arrange
        var request = new UpdateUser(Guid.Empty, "Test name");
        var validator = new UpdateUser.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_NameIsEmpty()
    {
        // Arrange
        var request = new UpdateUser(Guid.NewGuid(), string.Empty);
        var validator = new UpdateUser.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        // Arrange
        var request = new UpdateUser(Guid.NewGuid(), "Test name");
        var validator = new UpdateUser.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_UpdateUserInContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existingUser = new User { Id = Guid.NewGuid(), Name = "Old name" };
        var context = A.Fake<IPaymentManagerContext>();
        var usersDbSet = new[] { existingUser }.BuildMockDbSet();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        var logger = new FakeLogger<UpdateUser.Handler>();
        var request = new UpdateUser(existingUser.Id, "New name");
        var handler = new UpdateUser.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        A.CallTo(() => usersDbSet.Update(A<User>.That.Matches(u => u.Name == "New name"))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
        response.ShouldNotBeNull();
        response.Id.ShouldBe(existingUser.Id);
        response.Name.ShouldBe("New name");
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_UserDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        var usersDbSet = Array.Empty<User>().BuildMockDbSet();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        var logger = new FakeLogger<UpdateUser.Handler>();
        var request = new UpdateUser(Guid.NewGuid(), "Test name");
        var handler = new UpdateUser.Handler(context, logger);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<User>>(() => handler.Handle(request, cancellationToken));
    }
}
