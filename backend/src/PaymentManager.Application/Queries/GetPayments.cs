using FluentValidation;
using LinqKit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Queries.GetPayments;
using static PaymentManager.Application.Queries.GetPayments.Response;
using static PaymentManager.Domain.Entities.PaymentSchedule;

namespace PaymentManager.Application.Queries;

public record GetPayments(Guid UserId, DateOnly From, DateOnly To) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<GetPayments>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThanOrEqualTo(x => x.To);
        }
    }

    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetPayments, Response>
    {
        public async Task<Response> Handle(GetPayments request, CancellationToken cancellationToken)
        {
            var predicate = PredicateBuilder.New<Payment>(p => p.UserId == request.UserId);

            if (request.From != DateOnly.MinValue)
            {
                predicate = predicate.And(p => p.Schedule.StartDate <= request.From && p.Schedule.EndDate >= request.From);
            }

            if (request.To != DateOnly.MaxValue)
            {
                predicate = predicate.And(p => p.Schedule.StartDate <= request.To);
            }

            logger.LogInformation("Retrieving payments from {From} to {To}", request.From, request.To);
            var payments = await context.Payments
                .Include(p => p.Schedule)
                .Include(p => p.Source)
                .Where(predicate)
                .ToArrayAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} payments", payments.Length);

            var calculatedPaymentsOccurrences = GetPaymentDtos(payments, request.From, request.To).ToArray();

            logger.LogInformation("Calculated {Count} payments occurrences", calculatedPaymentsOccurrences.Length);

            return new Response(calculatedPaymentsOccurrences.OrderBy(p => p.Date).ThenBy(p => p.Id));
        }

        private static ICollection<PaymentDto> GetPaymentDtos(ICollection<Payment> payments, DateOnly from, DateOnly to)
            => [.. payments.Select(p => GetPaymentDtos(p, from, to)).SelectMany(x => x)];

        private static ICollection<PaymentDto> GetPaymentDtos(Payment payment, DateOnly from, DateOnly to)
            => payment.Schedule.Occurs switch
            {
                Frequency.Unknown => Array.Empty<PaymentDto>(),
                Frequency.Once => [new PaymentDto(payment.Id, payment.Name, payment.Description, payment.Amount, payment.Schedule.StartDate, payment.Source!.Name)],
                Frequency.Daily => GetDailyPaymentDto(payment, from, to),
                Frequency.Weekly => GetWeeklyPaymentDto(payment, from, to),
                Frequency.Monthly => GetMonthlyPaymentDto(payment, from, to),
                Frequency.Annually => GetAnnuallyPaymentDto(payment, from, to),
                _ => Array.Empty<PaymentDto>()
            };

        private static List<PaymentDto> GetPaymentDto(Payment payment, DateOnly from, DateOnly to, Func<DateOnly, DateOnly> incrementDate)
        {
            var lowerBoundary = GetLowerBoundary(payment.Schedule.StartDate, from, incrementDate);
            var upperBoundary = GetUpperBoundary(payment.Schedule.EndDate!.Value, to, incrementDate);
            var currentDate = lowerBoundary;
            var paymentDtos = new List<PaymentDto>();

            while (currentDate <= upperBoundary)
            {
                paymentDtos.Add(new PaymentDto(payment.Id, payment.Name, payment.Description, payment.Amount, currentDate, payment.Source!.Name));
                currentDate = incrementDate(currentDate);
            }

            return paymentDtos;
        }

        private static DateOnly GetLowerBoundary(DateOnly paymentStartDate, DateOnly from, Func<DateOnly, DateOnly> incrementDate)
        {
            if (paymentStartDate >= from)
            {
                return paymentStartDate;
            }

            var currentDate = paymentStartDate;
            while (currentDate < from)
            {
                currentDate = incrementDate(currentDate);
            }
            return currentDate;
        }

        private static DateOnly GetUpperBoundary(DateOnly paymentEndDate, DateOnly to, Func<DateOnly, DateOnly> incrementDate)
            => paymentEndDate <= to ? paymentEndDate : to;

        private static List<PaymentDto> GetDailyPaymentDto(Payment payment, DateOnly from, DateOnly to)
            => GetPaymentDto(payment, from, to, d => d.AddDays(1 * payment.Schedule.Every));

        private static List<PaymentDto> GetWeeklyPaymentDto(Payment payment, DateOnly from, DateOnly to)
            => GetPaymentDto(payment, from, to, d => d.AddDays(7 * payment.Schedule.Every));

        private static List<PaymentDto> GetMonthlyPaymentDto(Payment payment, DateOnly from, DateOnly to)
            => GetPaymentDto(payment, from, to, d => d.AddMonths(1 * payment.Schedule.Every));

        private static List<PaymentDto> GetAnnuallyPaymentDto(Payment payment, DateOnly from, DateOnly to)
            => GetPaymentDto(payment, from, to, d => d.AddYears(1 * payment.Schedule.Every));
    }

    public record Response(IOrderedEnumerable<PaymentDto> Payments)
    {
        public record PaymentDto(Guid Id, string Name, string? Description, decimal Amount, DateOnly Date, string Source);
    };
}
