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

internal sealed class CreatePersonTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_Empty()
    {
        var request = new CreatePerson(Guid.Empty, "Alice");
        var result = new CreatePerson.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Empty()
    {
        var request = new CreatePerson(Guid.NewGuid(), "");
        var result = new CreatePerson.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Over200Chars()
    {
        var request = new CreatePerson(Guid.NewGuid(), new string('a', 201));
        var result = new CreatePerson.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        var request = new CreatePerson(Guid.NewGuid(), "Alice");
        var result = new CreatePerson.Validator().TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_AddPersonToContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var people = Array.Empty<Person>();
        var peopleDbSet = people.BuildMockDbSet();
        A.CallTo(() => peopleDbSet.Add(A<Person>._)).Invokes((Person p) => people = people.Append(p).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(peopleDbSet);
        var logger = new FakeLogger<CreatePerson.Handler>();
        var request = new CreatePerson(Guid.NewGuid(), "Bob");
        var handler = new CreatePerson.Handler(context, logger);

        var response = await handler.Handle(request, cancellationToken);

        people.ShouldHaveSingleItem();
        people.First().UserId.ShouldBe(request.UserId);
        people.First().Name.ShouldBe("Bob");
        response.Id.ShouldNotBe(Guid.Empty);
        response.Name.ShouldBe("Bob");
    }
}

internal sealed class UpdatePersonTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForId_When_Empty()
    {
        var request = new UpdatePerson(Guid.Empty, Guid.NewGuid(), "Alice");
        var result = new UpdatePerson.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Empty()
    {
        var request = new UpdatePerson(Guid.NewGuid(), Guid.NewGuid(), "");
        var result = new UpdatePerson.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        var request = new UpdatePerson(Guid.NewGuid(), Guid.NewGuid(), "Alice");
        var result = new UpdatePerson.Validator().TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_UpdatePersonInContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new Person { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Old Name" };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(new[] { existing }.BuildMockDbSet());
        var logger = new FakeLogger<UpdatePerson.Handler>();
        var request = new UpdatePerson(existing.Id, existing.UserId, "New Name");
        var handler = new UpdatePerson.Handler(context, logger);

        var response = await handler.Handle(request, cancellationToken);

        A.CallTo(() => context.People.Update(A<Person>.That.Matches(p => p.Name == "New Name"))).MustHaveHappenedOnceExactly();
        response.Name.ShouldBe("New Name");
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PersonDoesNotExist()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(Array.Empty<Person>().BuildMockDbSet());
        var logger = new FakeLogger<UpdatePerson.Handler>();
        var request = new UpdatePerson(Guid.NewGuid(), Guid.NewGuid(), "Name");
        var handler = new UpdatePerson.Handler(context, logger);

        await Should.ThrowAsync<NotFoundException<Person>>(() => handler.Handle(request, cancellationToken));
    }
}

internal sealed class DeletePersonTests
{
    [Test]
    public async Task Handler_Handle_Should_RemovePersonFromContext_And_CleanUpMemberships()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new Person { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Alice" };
        var membership = new PayerGroupMember { PayerGroupId = Guid.NewGuid(), PersonId = existing.Id };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(new[] { existing }.BuildMockDbSet());
        A.CallTo(() => context.People.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<Person?>(existing));
        A.CallTo(() => context.PaymentSplits).Returns(Array.Empty<PaymentSplit>().BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentSplits).Returns(Array.Empty<EffectivePaymentSplit>().BuildMockDbSet());
        A.CallTo(() => context.PayerGroupMembers).Returns(new[] { membership }.BuildMockDbSet());
        var logger = new FakeLogger<DeletePerson.Handler>();
        var request = new DeletePerson(existing.Id);
        var handler = new DeletePerson.Handler(context, logger);

        await handler.Handle(request, cancellationToken);

        A.CallTo(() => context.People.Remove(existing)).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.PayerGroupMembers.RemoveRange(A<IEnumerable<PayerGroupMember>>.That.Matches(m => m.Contains(membership)))).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_PersonDoesNotExist()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(Array.Empty<Person>().BuildMockDbSet());
        A.CallTo(() => context.People.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<Person?>(default(Person)));
        var logger = new FakeLogger<DeletePerson.Handler>();
        var request = new DeletePerson(Guid.NewGuid());
        var handler = new DeletePerson.Handler(context, logger);

        await Should.ThrowAsync<NotFoundException<Person>>(() => handler.Handle(request, cancellationToken));
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowValidationException_When_PersonHoldsSplits()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new Person { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Alice" };
        var split = new PaymentSplit { PaymentId = Guid.NewGuid(), PersonId = existing.Id, Percentage = 50m };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(new[] { existing }.BuildMockDbSet());
        A.CallTo(() => context.People.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<Person?>(existing));
        A.CallTo(() => context.PaymentSplits).Returns(new[] { split }.BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentSplits).Returns(Array.Empty<EffectivePaymentSplit>().BuildMockDbSet());
        var logger = new FakeLogger<DeletePerson.Handler>();
        var request = new DeletePerson(existing.Id);
        var handler = new DeletePerson.Handler(context, logger);

        await Should.ThrowAsync<ValidationException>(() => handler.Handle(request, cancellationToken));
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowValidationException_When_PersonHoldsEffectiveSplits()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new Person { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Alice" };
        var split = new EffectivePaymentSplit
        {
            PaymentId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2025, 6, 1),
            PersonId = existing.Id,
            Percentage = 50m
        };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.People).Returns(new[] { existing }.BuildMockDbSet());
        A.CallTo(() => context.People.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<Person?>(existing));
        A.CallTo(() => context.PaymentSplits).Returns(Array.Empty<PaymentSplit>().BuildMockDbSet());
        A.CallTo(() => context.EffectivePaymentSplits).Returns(new[] { split }.BuildMockDbSet());
        var logger = new FakeLogger<DeletePerson.Handler>();
        var request = new DeletePerson(existing.Id);
        var handler = new DeletePerson.Handler(context, logger);

        await Should.ThrowAsync<ValidationException>(() => handler.Handle(request, cancellationToken));
    }
}
