using NUnit.Framework;
using PaymentManager.Application.Commands;
using FluentValidation.TestHelper;
using FakeItEasy;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using MockQueryable.FakeItEasy;
using Shouldly;
using Microsoft.Extensions.Logging.Testing;
using static PaymentManager.Application.Common.Exceptions;
using PaymentManager.Application.Common;

namespace PaymentManager.Application.Tests.Unit.Commands;

internal sealed class CreatePayerGroupTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_Empty()
    {
        var request = new CreatePayerGroup(Guid.Empty, "The Flat");
        var result = new CreatePayerGroup.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Empty()
    {
        var request = new CreatePayerGroup(Guid.NewGuid(), "");
        var result = new CreatePayerGroup.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Over200Chars()
    {
        var request = new CreatePayerGroup(Guid.NewGuid(), new string('a', 201));
        var result = new CreatePayerGroup.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        var request = new CreatePayerGroup(Guid.NewGuid(), "The Flat");
        var result = new CreatePayerGroup.Validator().TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_AddPayerGroupToContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var payerGroups = Array.Empty<PayerGroup>();
        var payerGroupsDbSet = payerGroups.BuildMockDbSet();
        A.CallTo(() => payerGroupsDbSet.Add(A<PayerGroup>._)).Invokes((PayerGroup h) => payerGroups = payerGroups.Append(h).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PayerGroups).Returns(payerGroupsDbSet);
        var logger = new FakeLogger<CreatePayerGroup.Handler>();
        var request = new CreatePayerGroup(Guid.NewGuid(), "The Flat");
        var handler = new CreatePayerGroup.Handler(context, logger);

        var response = await handler.Handle(request, cancellationToken);

        payerGroups.ShouldHaveSingleItem();
        payerGroups.First().UserId.ShouldBe(request.UserId);
        payerGroups.First().Name.ShouldBe("The Flat");
        response.Id.ShouldNotBe(Guid.Empty);
        response.Name.ShouldBe("The Flat");
    }
}

internal sealed class UpdatePayerGroupTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForId_When_Empty()
    {
        var request = new UpdatePayerGroup(Guid.Empty, Guid.NewGuid(), "The Flat");
        var result = new UpdatePayerGroup.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Empty()
    {
        var request = new UpdatePayerGroup(Guid.NewGuid(), Guid.NewGuid(), "");
        var result = new UpdatePayerGroup.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        var request = new UpdatePayerGroup(Guid.NewGuid(), Guid.NewGuid(), "The Flat");
        var result = new UpdatePayerGroup.Validator().TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_UpdatePayerGroupInContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new PayerGroup { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Old Name" };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PayerGroups).Returns(new[] { existing }.BuildMockDbSet());
        var logger = new FakeLogger<UpdatePayerGroup.Handler>();
        var request = new UpdatePayerGroup(existing.Id, existing.UserId, "New Name");
        var handler = new UpdatePayerGroup.Handler(context, logger);

        var response = await handler.Handle(request, cancellationToken);

        A.CallTo(() => context.PayerGroups.Update(A<PayerGroup>.That.Matches(h => h.Name == "New Name"))).MustHaveHappenedOnceExactly();
        response.Name.ShouldBe("New Name");
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PayerGroupDoesNotExist()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PayerGroups).Returns(Array.Empty<PayerGroup>().BuildMockDbSet());
        var logger = new FakeLogger<UpdatePayerGroup.Handler>();
        var request = new UpdatePayerGroup(Guid.NewGuid(), Guid.NewGuid(), "Name");
        var handler = new UpdatePayerGroup.Handler(context, logger);

        await Should.ThrowAsync<NotFoundException<PayerGroup>>(() => handler.Handle(request, cancellationToken));
    }
}

internal sealed class DeletePayerGroupTests
{
    [Test]
    public async Task Handler_Handle_Should_RemovePayerGroupFromContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new PayerGroup { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "The Flat" };
        var membership = new PayerGroupMember { PayerGroupId = existing.Id, PersonId = Guid.NewGuid() };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PayerGroups).Returns(new[] { existing }.BuildMockDbSet());
        A.CallTo(() => context.PayerGroups.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<PayerGroup?>(existing));
        A.CallTo(() => context.Payments).Returns(Array.Empty<Payment>().BuildMockDbSet());
        A.CallTo(() => context.PayerGroupMembers).Returns(new[] { membership }.BuildMockDbSet());
        var logger = new FakeLogger<DeletePayerGroup.Handler>();
        var request = new DeletePayerGroup(existing.Id);
        var handler = new DeletePayerGroup.Handler(context, logger);

        await handler.Handle(request, cancellationToken);

        A.CallTo(() => context.PayerGroups.Remove(existing)).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.PayerGroupMembers.RemoveRange(A<IEnumerable<PayerGroupMember>>.That.Matches(m => m.Contains(membership)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PayerGroupDoesNotExist()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PayerGroups).Returns(Array.Empty<PayerGroup>().BuildMockDbSet());
        A.CallTo(() => context.PayerGroups.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<PayerGroup?>(default(PayerGroup)));
        var logger = new FakeLogger<DeletePayerGroup.Handler>();
        var request = new DeletePayerGroup(Guid.NewGuid());
        var handler = new DeletePayerGroup.Handler(context, logger);

        await Should.ThrowAsync<NotFoundException<PayerGroup>>(() => handler.Handle(request, cancellationToken));
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowValidationException_When_GroupStillHasPayments()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new PayerGroup { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "The Flat" };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            PaymentSourceId = Guid.NewGuid(),
            PayeeId = Guid.NewGuid(),
            InitialAmount = 100m,
            Currency = "USD",
            Frequency = PaymentFrequency.Monthly,
            Direction = PaymentDirection.Outgoing,
            StartDate = new DateOnly(2025, 1, 1),
            PayerGroupId = existing.Id
        };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.PayerGroups).Returns(new[] { existing }.BuildMockDbSet());
        A.CallTo(() => context.PayerGroups.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<PayerGroup?>(existing));
        A.CallTo(() => context.Payments).Returns(new[] { payment }.BuildMockDbSet());
        var logger = new FakeLogger<DeletePayerGroup.Handler>();
        var request = new DeletePayerGroup(existing.Id);
        var handler = new DeletePayerGroup.Handler(context, logger);

        await Should.ThrowAsync<ValidationException>(() => handler.Handle(request, cancellationToken));
        A.CallTo(() => context.PayerGroups.Remove(A<PayerGroup>._)).MustNotHaveHappened();
    }
}
