using NUnit.Framework;
using PaymentManager.Application.Commands;
using FluentValidation.TestHelper;
using FakeItEasy;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using MockQueryable.FakeItEasy;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class CreatePaymentSourceTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_UserIdIsEmpty()
    {
        // Arrange
        var request = new CreatePaymentSource(Guid.Empty, "Test source");
        var validator = new CreatePaymentSource.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_NameIsEmpty()
    {
        // Arrange
        var request = new CreatePaymentSource(Guid.NewGuid(), string.Empty);
        var validator = new CreatePaymentSource.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        // Arrange
        var request = new CreatePaymentSource(Guid.NewGuid(), "Test source");
        var validator = new CreatePaymentSource.Validator();

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_AddPaymentSourceToContext()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var paymentSources = Array.Empty<PaymentSource>();
        var paymentSourcesDbSet = paymentSources.BuildMockDbSet();
        A.CallTo(() => paymentSourcesDbSet.Add(A<PaymentSource>._)).Invokes((PaymentSource ps) => paymentSources = paymentSources.Append(ps).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PaymentSources).Returns(paymentSourcesDbSet);
        var logger = new FakeLogger<CreatePaymentSource.Handler>();
        var userId = Guid.NewGuid();
        var request = new CreatePaymentSource(userId, "Test source");
        var handler = new CreatePaymentSource.Handler(context, logger);

        // Act
        var response = await handler.Handle(request, cancellationToken);

        // Assert
        paymentSources.ShouldNotBeNull();
        paymentSources.ShouldHaveSingleItem();
        paymentSources.First().UserId.ShouldBe(request.UserId);
        paymentSources.First().Name.ShouldBe(request.Name);
        response.ShouldNotBeNull();
        response.Id.ShouldNotBe(Guid.Empty);
        response.UserId.ShouldBe(request.UserId);
        response.Name.ShouldBe(request.Name);
    }
}
