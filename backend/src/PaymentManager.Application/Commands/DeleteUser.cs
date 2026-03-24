using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record DeleteUser(Guid Id) : IRequest
{
    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<DeleteUser, Unit>
    {
        public async Task<Unit> Handle(DeleteUser request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting user '{Id}'", request.Id);
            var user = await context.Users.FindAsync([request.Id], cancellationToken);

            if (user is null)
            {
                throw new NotFoundException<User>($"Id: {request.Id}");
            }

            context.Users.Remove(user);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Deleted user '{Id}'", request.Id);
            return Unit.Value;
        }
    }
}
