using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreateContact;

namespace PaymentManager.Application.Commands;

public record CreateContact(Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreateContact>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreateContact, Response>
    {
        public async Task<Response> Handle(CreateContact request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating contact with name: '{Name}' for user '{UserId}'", request.Name, request.UserId);
            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Name = request.Name
            };

            context.Contacts.Add(contact);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created contact '{Id}' with name: '{Name}'", contact.Id, contact.Name);

            return new Response(contact.Id, contact.UserId, contact.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
