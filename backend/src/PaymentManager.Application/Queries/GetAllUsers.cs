using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using static PaymentManager.Application.Queries.GetAllUsers;
using static PaymentManager.Application.Queries.GetAllUsers.Response;

namespace PaymentManager.Application.Queries;

public record GetAllUsers : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllUsers, Response>
    {
        public async Task<Response> Handle(GetAllUsers request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all users...");
            var users = await context.Users.Select(x => new UserDto(x.Id, x.Name)).ToArrayAsync(cancellationToken);
            logger.LogInformation("Successfully fetched {count} all users", users.Length);
            return new Response([.. users.OrderBy(u => u.Id)]);
        }
    }
    public record Response(ICollection<UserDto> Users)
    {
        public record UserDto(Guid Id, string Name);
    };
}
