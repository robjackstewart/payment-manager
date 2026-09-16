using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.UpdatePerson;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record UpdatePerson(Guid Id, Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<UpdatePerson>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdatePerson, Response>
    {
        public async Task<Response> Handle(UpdatePerson request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating person '{Id}'", request.Id);
            var person = await context.People
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (person is null)
            {
                throw new NotFoundException<Person>($"Id: {request.Id}");
            }

            person = person with { UserId = request.UserId, Name = request.Name };

            context.People.Update(person);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Updated person '{Id}' with name: '{Name}'", person.Id, person.Name);

            return new Response(person.Id, person.UserId, person.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
