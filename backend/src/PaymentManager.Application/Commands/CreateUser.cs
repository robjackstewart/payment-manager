using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreateUser;

namespace PaymentManager.Application.Commands;

public record CreateUser(string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreateUser>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreateUser, Response>
    {
        public async Task<Response> Handle(CreateUser request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating user with name: '{Name}'", request.Name);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name
            };

            context.Users.Add(user);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created user '{Id}' with name: '{Name}'", user.Name, user.Id);

            return new Response(user.Id, user.Name);
        }
    }

    public record Response(Guid Id, string Name);
}
