using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using static PaymentManager.Application.Queries.GetAllPayees;
using static PaymentManager.Application.Queries.GetAllPayees.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPayees(Guid UserId) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllPayees, Response>
    {
        public async Task<Response> Handle(GetAllPayees request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all payees for user '{UserId}'...", request.UserId);
            var payees = await context.Payees.Where(x => x.UserId == request.UserId).Select(x => new PayeeDto(x.Id, x.UserId, x.Name)).ToArrayAsync(cancellationToken);
            logger.LogInformation("Successfully fetched {count} payees for user '{UserId}'", payees.Length, request.UserId);
            return new Response([.. payees.OrderBy(p => p.Id)]);
        }
    }
    public record Response(ICollection<PayeeDto> Payees)
    {
        public record PayeeDto(Guid Id, Guid UserId, string Name);
    };
}
