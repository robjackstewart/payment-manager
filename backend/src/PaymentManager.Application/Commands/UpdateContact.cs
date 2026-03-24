using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Commands.UpdateContact;
using PaymentManager.Application.Common;

namespace PaymentManager.Application.Commands;

public record UpdateContact(Guid Id, Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<UpdateContact>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdateContact, Response>
    {
        public async Task<Response> Handle(UpdateContact request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating contact '{Id}'", request.Id);
            var contact = await context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contact is null)
            {
                throw new NotFoundException<Contact>($"Id: {request.Id}");
            }

            contact = contact with { UserId = request.UserId, Name = request.Name };

            context.Contacts.Update(contact);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Updated contact '{Id}' with name: '{Name}'", contact.Id, contact.Name);

            return new Response(contact.Id, contact.UserId, contact.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
