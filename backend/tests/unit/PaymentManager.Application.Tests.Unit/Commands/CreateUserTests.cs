using NUnit.Framework;
using PaymentManager.Application.Commands;
using FluentValidation.TestHelper;
using FakeItEasy;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using MockQueryable.FakeItEasy;
using Microsoft.Extensions.Logging.Testing;
using FluentAssertions;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class CreateUserTests
{

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_NameIsEmpty()
    {
        // Arrange
        var request = new CreateUser(string.Empty);
        var validator = new CreateUser.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrorForName_When_NameIsNotEmpty()
    {
        // Arrange
        var request = new CreateUser("Test name");
        var validator = new CreateUser.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Hander_Handle_Should_AddUserToContext()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var users = Array.Empty<User>();
        var usersDbSet = users.BuildMockDbSet();
        A.CallTo(() => usersDbSet.Add(A<User>._)).Invokes((User user) => users = users.Append(user).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Users).Returns(usersDbSet);
        var logger = new FakeLogger<CreateUser.Handler>();
        var request = new CreateUser("Test name");
        var handler = new CreateUser.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        users.Should().NotBeNull();
        users.Should().HaveCount(1);
        users.First().Name.Should().Be(request.Name);
        response.Should().NotBeNull();
        response.Id.Should().NotBe(Guid.Empty);
        response.Name.Should().Be(request.Name);
    }
}
