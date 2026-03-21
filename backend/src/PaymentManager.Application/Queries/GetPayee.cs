using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Queries.GetPayee;

namespace PaymentManager.Application.Queries;

public record GetPayee(Guid Id) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext readOnlyPaymentManagerContext, ILogger<Handler> logger) : IRequestHandler<GetPayee, Response>
    {
        public async Task<Response> Handle(GetPayee request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting payee with id '{Id}'", request.Id);
            var payee = await readOnlyPaymentManagerContext.Payees.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (payee is null)
            {
                logger.LogWarning("Payee with Id '{Id}' was not found", request.Id);
                throw new NotFoundException<Payee>($"{nameof(payee.Id)} is '{request.Id}'");
            }
            logger.LogInformation("Successfully found payee with Id '{Id}'", payee.Id);
            return new Response(payee.Id, payee.UserId, payee.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
