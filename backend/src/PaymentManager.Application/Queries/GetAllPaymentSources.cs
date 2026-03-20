using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using static PaymentManager.Application.Queries.GetAllPaymentSources;
using static PaymentManager.Application.Queries.GetAllPaymentSources.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPaymentSources(Guid UserId) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllPaymentSources, Response>
    {
        public async Task<Response> Handle(GetAllPaymentSources request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all payment sources for user: '{UserId}'...", request.UserId);
            var paymentSources = await context.PaymentSources
                .Where(x => x.UserId == request.UserId)
                .Select(x => new PaymentSourceDto(x.Id, x.UserId, x.Name))
                .ToArrayAsync(cancellationToken);
            logger.LogInformation("Successfully fetched {count} payment sources for user: '{UserId}'", paymentSources.Length, request.UserId);
            return new Response([.. paymentSources.OrderBy(p => p.Id)]);
        }
    }

    public record Response(ICollection<PaymentSourceDto> PaymentSources)
    {
        public record PaymentSourceDto(Guid Id, Guid UserId, string Name);
    };
}
