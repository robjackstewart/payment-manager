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

internal sealed class CreateContactTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForUserId_When_Empty()
    {
        var request = new CreateContact(Guid.Empty, "Alice");
        var result = new CreateContact.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Empty()
    {
        var request = new CreateContact(Guid.NewGuid(), "");
        var result = new CreateContact.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Over200Chars()
    {
        var request = new CreateContact(Guid.NewGuid(), new string('a', 201));
        var result = new CreateContact.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        var request = new CreateContact(Guid.NewGuid(), "Alice");
        var result = new CreateContact.Validator().TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_AddContactToContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var contacts = Array.Empty<Contact>();
        var contactsDbSet = contacts.BuildMockDbSet();
        A.CallTo(() => contactsDbSet.Add(A<Contact>._)).Invokes((Contact c) => contacts = contacts.Append(c).ToArray());
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Contacts).Returns(contactsDbSet);
        var logger = new FakeLogger<CreateContact.Handler>();
        var request = new CreateContact(Guid.NewGuid(), "Bob");
        var handler = new CreateContact.Handler(context, logger);

        var response = await handler.Handle(request, cancellationToken);

        contacts.ShouldHaveSingleItem();
        contacts.First().UserId.ShouldBe(request.UserId);
        contacts.First().Name.ShouldBe("Bob");
        response.Id.ShouldNotBe(Guid.Empty);
        response.Name.ShouldBe("Bob");
    }
}

internal sealed class UpdateContactTests
{
    [Test]
    public void Validator_Should_HaveValidationErrorForId_When_Empty()
    {
        var request = new UpdateContact(Guid.Empty, Guid.NewGuid(), "Alice");
        var result = new UpdateContact.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Test]
    public void Validator_Should_HaveValidationErrorForName_When_Empty()
    {
        var request = new UpdateContact(Guid.NewGuid(), Guid.NewGuid(), "");
        var result = new UpdateContact.Validator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Validator_Should_NotHaveValidationErrors_When_RequestIsValid()
    {
        var request = new UpdateContact(Guid.NewGuid(), Guid.NewGuid(), "Alice");
        var result = new UpdateContact.Validator().TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_Handle_Should_UpdateContactInContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new Contact { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Old Name" };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Contacts).Returns(new[] { existing }.BuildMockDbSet());
        var logger = new FakeLogger<UpdateContact.Handler>();
        var request = new UpdateContact(existing.Id, existing.UserId, "New Name");
        var handler = new UpdateContact.Handler(context, logger);

        var response = await handler.Handle(request, cancellationToken);

        A.CallTo(() => context.Contacts.Update(A<Contact>.That.Matches(c => c.Name == "New Name"))).MustHaveHappenedOnceExactly();
        response.Name.ShouldBe("New Name");
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_ContactDoesNotExist()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Contacts).Returns(Array.Empty<Contact>().BuildMockDbSet());
        var logger = new FakeLogger<UpdateContact.Handler>();
        var request = new UpdateContact(Guid.NewGuid(), Guid.NewGuid(), "Name");
        var handler = new UpdateContact.Handler(context, logger);

        await Should.ThrowAsync<NotFoundException<Contact>>(() => handler.Handle(request, cancellationToken));
    }
}

internal sealed class DeleteContactTests
{
    [Test]
    public async Task Handler_Handle_Should_RemoveContactFromContext()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var existing = new Contact { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Alice" };
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Contacts).Returns(new[] { existing }.BuildMockDbSet());
        A.CallTo(() => context.Contacts.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<Contact?>(existing));
        var logger = new FakeLogger<DeleteContact.Handler>();
        var request = new DeleteContact(existing.Id);
        var handler = new DeleteContact.Handler(context, logger);

        await handler.Handle(request, cancellationToken);

        A.CallTo(() => context.Contacts.Remove(existing)).MustHaveHappenedOnceExactly();
        A.CallTo(() => context.SaveChanges(cancellationToken)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handler_Handle_Should_ThrowNotFoundException_When_ContactDoesNotExist()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        var context = A.Fake<IPaymentManagerContext>();
        A.CallTo(() => context.Contacts).Returns(Array.Empty<Contact>().BuildMockDbSet());
        A.CallTo(() => context.Contacts.FindAsync(A<object[]>._, A<CancellationToken>._)).Returns(new ValueTask<Contact?>(default(Contact)));
        var logger = new FakeLogger<DeleteContact.Handler>();
        var request = new DeleteContact(Guid.NewGuid());
        var handler = new DeleteContact.Handler(context, logger);

        await Should.ThrowAsync<NotFoundException<Contact>>(() => handler.Handle(request, cancellationToken));
    }
}
