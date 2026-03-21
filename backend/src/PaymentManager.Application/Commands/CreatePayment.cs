using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Commands.CreatePayment;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record CreatePayment(Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description = null, IReadOnlyList<CreatePayment.SplitRequest>? Splits = null) : IRequest<Response>
{
    public record SplitRequest(Guid ContactId, decimal Percentage);

    internal sealed class Validator : AbstractValidator<CreatePayment>
    {
        public Validator()
        {
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

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreatePayment, Response>
    {
        public async Task<Response> Handle(CreatePayment request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating payment for user: '{UserId}' with amount: '{Amount}'", request.UserId, request.Amount);

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

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
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

            context.Payments.Add(payment);

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
                }
            }

            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created payment '{Id}' for user: '{UserId}' with amount: '{Amount}'", payment.Id, payment.UserId, payment.Amount);

            var splits = request.Splits?.Select(s => new Response.SplitDto(s.ContactId, string.Empty, s.Percentage)).ToList()
                ?? [];
            return new Response(payment.Id, payment.UserId, payment.PaymentSourceId, payment.PayeeId, payment.Amount, payment.Currency, payment.Frequency, payment.StartDate, payment.EndDate, payment.Description, splits);
        }
    }

    public record Response(Guid Id, Guid UserId, Guid PaymentSourceId, Guid PayeeId, decimal Amount, string Currency, PaymentFrequency Frequency, DateOnly StartDate, DateOnly? EndDate, string? Description, ICollection<Response.SplitDto> Splits)
    {
        public record SplitDto(Guid ContactId, string ContactName, decimal Percentage);
    }
}
