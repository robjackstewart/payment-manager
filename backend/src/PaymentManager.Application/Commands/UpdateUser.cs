using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Commands.UpdateUser;
using PaymentManager.Application.Common;

namespace PaymentManager.Application.Commands;

public record UpdateUser(Guid Id, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<UpdateUser>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdateUser, Response>
    {
        public async Task<Response> Handle(UpdateUser request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating user '{Id}'", request.Id);
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException<User>($"Id: {request.Id}");
            }

            user = user with
            {
                Name = request.Name
            };

            context.Users.Update(user);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Updated user '{Id}' with name: '{Name}'", user.Id, user.Name);

            return new Response(user.Id, user.Name);
        }
    }

    public record Response(Guid Id, string Name);
}
