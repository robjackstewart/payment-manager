using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using static PaymentManager.Application.Queries.GetAllPaymentSources;
using static PaymentManager.Application.Queries.GetAllPaymentSources.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPaymentSources : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext Context, ILogger<Handler> Logger) : IRequestHandler<GetAllPaymentSources, Response>
    {
        public async Task<Response> Handle(GetAllPaymentSources request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Getting all payment sources...");

            var paymentSources = await Context.PaymentSources
                .Select(x => new PaymentSourceDto(x.Id, x.Name, x.Description))
                .ToArrayAsync(cancellationToken);

            Logger.LogInformation("Found {Count} payment sources.", paymentSources.Length);

            return new Response(paymentSources);
        }
    }

    public record Response(ICollection<PaymentSourceDto> PaymentSources)
    {
        public record PaymentSourceDto(Guid Id, string Name, string? Description);
    };
}
