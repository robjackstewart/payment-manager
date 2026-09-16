using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using static PaymentManager.Application.Queries.GetAllPeople;
using static PaymentManager.Application.Queries.GetAllPeople.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPeople(Guid UserId) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllPeople, Response>
    {
        public async Task<Response> Handle(GetAllPeople request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all people for user '{UserId}'...", request.UserId);
            var people = await context.People
                .Where(x => x.UserId == request.UserId)
                .OrderBy(x => x.Name)
                .Select(x => new PersonDto(x.Id, x.UserId, x.Name))
                .ToArrayAsync(cancellationToken);
            logger.LogInformation("Successfully fetched {Count} people for user '{UserId}'", people.Length, request.UserId);

            return new Response([.. people]);
        }
    }

    public record Response(ICollection<PersonDto> People)
    {
        public record PersonDto(Guid Id, Guid UserId, string Name);
    }
}
