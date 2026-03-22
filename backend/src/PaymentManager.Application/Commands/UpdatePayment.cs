using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Commands.UpdatePayment;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record UpdatePayment(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description = null, IReadOnlyList<UpdatePayment.SplitRequest>? Splits = null) : IRequest<Response>
{
    public record SplitRequest(Guid ContactId, decimal Percentage);

    internal sealed class Validator : AbstractValidator<UpdatePayment>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.PaymentSourceId).NotEmpty();
            RuleFor(x => x.PayeeId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
            RuleFor(x => x.Frequency).IsInEnum();
            RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
            RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
            RuleFor(x => x.EndDate).Must(endDate => endDate is null).When(x => x.Frequency == PaymentFrequency.Once)
                .WithMessage("EndDate must be null when Frequency is Once.");
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
            When(x => x.Splits is { Count: > 0 }, () =>
            {
                RuleForEach(x => x.Splits).ChildRules(split =>
                {
                    split.RuleFor(s => s.ContactId).NotEmpty();
                    split.RuleFor(s => s.Percentage).GreaterThan(0).LessThanOrEqualTo(100);
                });
                RuleFor(x => x.Splits).Must(splits => splits!.Sum(s => s.Percentage) <= 100)
                    .WithMessage("Total split percentage cannot exceed 100.");
            });
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdatePayment, Response>
    {
        public async Task<Response> Handle(UpdatePayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating payment '{Id}'", request.Id);
            var payment = await context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (payment is null)
            {
                throw new NotFoundException<Payment>($"Id: {request.Id}");
            }

            if (request.Splits is { Count: > 0 })
            {
                var requestedContactIds = request.Splits.Select(s => s.ContactId).ToHashSet();
                var existingContactIds = await context.Contacts
                    .Where(c => requestedContactIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToHashSetAsync(cancellationToken);
                var missingIds = requestedContactIds.Except(existingContactIds).ToList();
                if (missingIds.Count > 0)
                {
                    throw new NotFoundException<Contact>($"ContactIds not found: {string.Join(", ", missingIds)}");
                }
            }

            payment = payment with
            {
                UserId = request.UserId,
                PaymentSourceId = request.PaymentSourceId,
                PayeeId = request.PayeeId,
                Amount = request.Amount,
                Currency = request.Currency,
                Frequency = request.Frequency,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Description = request.Description
            };

            context.Payments.Update(payment);

            var existingSplits = await context.PaymentSplits
                .Where(s => s.PaymentId == payment.Id)
                .ToListAsync(cancellationToken);
            context.PaymentSplits.RemoveRange(existingSplits);

            List<Response.SplitDto> splitDtos = [];
            if (request.Splits is { Count: > 0 })
            {
                foreach (var split in request.Splits)
                {
                    context.PaymentSplits.Add(new PaymentSplit
                    {
                        PaymentId = payment.Id,
                        ContactId = split.ContactId,
                        Percentage = split.Percentage
                    });
                    splitDtos.Add(new Response.SplitDto(split.ContactId, split.Percentage,
                        SplitPaymentCalculator.CalculateValue(request.Amount, split.Percentage)));
                }
            }

            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Updated payment '{Id}' for user: '{UserId}' with amount: '{Amount}'", payment.Id, payment.UserId, payment.Amount);

            var userSharePct = SplitPaymentCalculator.UserSharePercentage(splitDtos.Select(s => s.Percentage));
            var userShare = new UserShareDto(userSharePct, SplitPaymentCalculator.CalculateValue(request.Amount, userSharePct));
            return new Response(payment.Id, payment.UserId, payment.PaymentSourceId, payment.PayeeId, payment.Amount, payment.Currency, payment.Frequency, payment.StartDate, payment.EndDate, payment.Description, userShare, splitDtos);
        }
    }

    public record Response(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description, UserShareDto UserShare, ICollection<Response.SplitDto> Splits)
    {
        public record SplitDto(Guid ContactId, decimal Percentage, decimal Value);
    }
}
