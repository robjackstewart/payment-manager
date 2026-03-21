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
                .ToArrayAsync(cancellationToken);

            var paymentIds = payments.Select(p => p.Id).ToHashSet();

            var splitRows = await context.PaymentSplits
                .Where(s => paymentIds.Contains(s.PaymentId))
                .Join(context.Contacts, s => s.ContactId, c => c.Id,
                    (s, c) => new { s.PaymentId, s.ContactId, c.Name, s.Percentage })
                .ToListAsync(cancellationToken);

            var splitsByPayment = splitRows
                .GroupBy(s => s.PaymentId)
                .ToDictionary(
                    g => g.Key,
                    g => (ICollection<PaymentDto.SplitDto>)g.Select(s => new PaymentDto.SplitDto(s.ContactId, s.Name, s.Percentage)).ToList());

            var paymentDtos = payments
                .OrderBy(p => p.Id)
                .Select(p => new PaymentDto(
                    p.Id, p.UserId, p.PaymentSourceId, p.PayeeId,
                    p.Amount, p.Currency, p.Frequency, p.StartDate, p.EndDate, p.Description,
                    splitsByPayment.GetValueOrDefault(p.Id) ?? []))
                .ToArray();

            logger.LogInformation("Successfully fetched {count} payments for user: '{UserId}'", paymentDtos.Length, request.UserId);
            return new Response([.. paymentDtos]);
        }
    }

    public record Response(ICollection<PaymentDto> Payments)
    {
        public record PaymentDto(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description, ICollection<PaymentDto.SplitDto> Splits)
        {
            public record SplitDto(Guid ContactId, string ContactName, decimal Percentage);
        }
    }
}
