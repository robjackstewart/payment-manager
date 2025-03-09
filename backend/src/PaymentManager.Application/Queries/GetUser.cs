using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Queries.GetUser;

namespace PaymentManager.Application.Queries;

public record GetUser(Guid Id) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext readOnlyPaymentManagerContext, ILogger<Handler> logger) : IRequestHandler<GetUser, Response>
    {
        public async Task<Response> Handle(GetUser request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting user with id '{Id}'", request.Id);
            var user = await readOnlyPaymentManagerContext.Users.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("User with Id '{Id}' was not found", request.Id);
                throw new NotFoundException<User>($"{nameof(user.Id)} is '{request.Id}'");
            }
            logger.LogInformation("Successfully found user with Id '{Id}'", user.Id);
            return new Response(user.Id, user.Name);
        }
    }

    public record Response(Guid Id, string Name);
}
