using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Queries.GetPayment;

namespace PaymentManager.Application.Queries;

public record GetPayment(Guid Id) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetPayment, Response>
    {
        public async Task<Response> Handle(GetPayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching payment with id: '{Id}'", request.Id);
            var payment = await context.Payments.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (payment is null)
            {
                throw new NotFoundException<Payment>($"Id: {request.Id}");
            }

            logger.LogInformation("Successfully fetched payment '{Id}'", payment.Id);
            return new Response(payment.Id, payment.UserId, payment.PaymentSourceId, payment.PayeeId, payment.Amount, payment.Currency, payment.Frequency, payment.StartDate, payment.EndDate, payment.Description);
        }
    }

    public record Response(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description);
}
