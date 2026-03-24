using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Queries.GetPaymentSource;

namespace PaymentManager.Application.Queries;

public record GetPaymentSource(Guid Id) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetPaymentSource, Response>
    {
        public async Task<Response> Handle(GetPaymentSource request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching payment source with id: '{Id}'", request.Id);
            var paymentSource = await context.PaymentSources.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (paymentSource is null)
            {
                throw new NotFoundException<PaymentSource>($"Id: {request.Id}");
            }

            logger.LogInformation("Successfully fetched payment source '{Id}'", paymentSource.Id);
            return new Response(paymentSource.Id, paymentSource.UserId, paymentSource.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
