using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record DeleteContact(Guid Id) : IRequest
{
    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<DeleteContact, Unit>
    {
        public async Task<Unit> Handle(DeleteContact request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting contact '{Id}'", request.Id);
            var contact = await context.Contacts.FindAsync([request.Id], cancellationToken);

            if (contact is null)
            {
                throw new NotFoundException<Contact>($"Id: {request.Id}");
            }

            context.Contacts.Remove(contact);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Deleted contact '{Id}'", contact.Id);
            return Unit.Value;
        }
    }
}
