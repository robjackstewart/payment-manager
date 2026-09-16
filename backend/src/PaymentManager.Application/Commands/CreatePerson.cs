using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreatePerson;

namespace PaymentManager.Application.Commands;

public record CreatePerson(Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreatePerson>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreatePerson, Response>
    {
        public async Task<Response> Handle(CreatePerson request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating person with name: '{Name}' for user '{UserId}'", request.Name, request.UserId);

            var person = new Person
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Name = request.Name
            };

            context.People.Add(person);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created person '{Id}' with name: '{Name}'", person.Id, person.Name);

            return new Response(person.Id, person.UserId, person.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
