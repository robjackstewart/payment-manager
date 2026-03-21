using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Queries.GetAllPayments;
using static PaymentManager.Application.Queries.GetAllPayments.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPayments(Guid UserId) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllPayments, Response>
    {
        public async Task<Response> Handle(GetAllPayments request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all payments for user: '{UserId}'...", request.UserId);
            var payments = await context.Payments
                .Where(x => x.UserId == request.UserId)
                .Select(x => new PaymentDto(x.Id, x.UserId, x.PaymentSourceId, x.PayeeId, x.Amount, x.Currency, x.Frequency, x.StartDate, x.EndDate, x.Description))
                .ToArrayAsync(cancellationToken);
            logger.LogInformation("Successfully fetched {count} payments for user: '{UserId}'", payments.Length, request.UserId);
            return new Response([.. payments.OrderBy(p => p.Id)]);
        }
    }

    public record Response(ICollection<PaymentDto> Payments)
    {
        public record PaymentDto(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description);
    };
}
