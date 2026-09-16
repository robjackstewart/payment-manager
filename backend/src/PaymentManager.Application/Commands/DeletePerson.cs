using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Common.Validation;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using DomainValidationException = PaymentManager.Application.Common.Exceptions.ValidationException;

namespace PaymentManager.Application.Commands;

public record DeletePerson(Guid Id) : IRequest
{
    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<DeletePerson, Unit>
    {
        public async Task<Unit> Handle(DeletePerson request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting person '{Id}'", request.Id);
            var person = await context.People.FindAsync([request.Id], cancellationToken);

            if (person is null)
            {
                throw new NotFoundException<Person>($"Id: {request.Id}");
            }

            // PaymentSplit -> Person is Restrict, so surface the reason rather than letting the
            // database reject it as an unhandled failure.
            var holdsSplits = await context.PaymentSplits
                .AnyAsync(s => s.PersonId == person.Id, cancellationToken);
            if (holdsSplits)
            {
                throw Invalid("Id", "This person still has a share of one or more payments. Remove them from those payments first.");
            }

            var memberships = await context.PayerGroupMembers
                .Where(m => m.PersonId == person.Id)
                .ToListAsync(cancellationToken);
            context.PayerGroupMembers.RemoveRange(memberships);

            context.People.Remove(person);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Deleted person '{Id}'", person.Id);
            return Unit.Value;
        }

        private static DomainValidationException Invalid(string propertyName, string error) =>
            new([new ValidationError { PropertyName = propertyName, Errors = [error] }]);
    }
}
