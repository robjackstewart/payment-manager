using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Commands.CreatePayment;

namespace PaymentManager.Application.Commands;

public record CreatePayment(Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreatePayment>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.PaymentSourceId).NotEmpty();
            RuleFor(x => x.PayeeId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Frequency).IsInEnum();
            RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
            RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
            RuleFor(x => x.EndDate).Must(endDate => endDate is null).When(x => x.Frequency == PaymentFrequency.Once)
                .WithMessage("EndDate must be null when Frequency is Once.");
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreatePayment, Response>
    {
        public async Task<Response> Handle(CreatePayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating payment for user: '{UserId}' with amount: '{Amount}'", request.UserId, request.Amount);
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                PaymentSourceId = request.PaymentSourceId,
                PayeeId = request.PayeeId,
                Amount = request.Amount,
                Frequency = request.Frequency,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            context.Payments.Add(payment);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created payment '{Id}' for user: '{UserId}' with amount: '{Amount}'", payment.Id, payment.UserId, payment.Amount);

            return new Response(payment.Id, payment.UserId, payment.PaymentSourceId, payment.PayeeId, payment.Amount, payment.Frequency, payment.StartDate, payment.EndDate);
        }
    }

    public record Response(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate);
}
