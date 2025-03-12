using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Queries.GetPaymentSource;

namespace PaymentManager.Application.Queries;

public record GetPaymentSource(Guid Id) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext Context, ILogger<Handler> Logger) : IRequestHandler<GetPaymentSource, Response>
    {
        public async Task<Response> Handle(GetPaymentSource request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Getting payment source '{Id}'...", request.Id);
            var paymentSource = await Context.PaymentSources.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (paymentSource is null)
            {
                Logger.LogWarning("Payment source '{Id}' not found.", request.Id);
                throw new NotFoundException<PaymentSource>($"Id is '{request.Id}'");
            }

            Logger.LogInformation("Payment source '{Id}' found.", request.Id);

            return new Response(paymentSource.Id, paymentSource.Name, paymentSource.Description);
        }
    }

    public record Response(Guid Id, string Name, string? Description);
}
