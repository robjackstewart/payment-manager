using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Application.Common.Validation;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.SetPayerGroupMembers;
using static PaymentManager.Application.Common.Exceptions;
using DomainValidationException = PaymentManager.Application.Common.Exceptions.ValidationException;

namespace PaymentManager.Application.Commands;

/// <summary>Replaces a payer group's membership wholesale with the supplied set of people.</summary>
public record SetPayerGroupMembers(Guid PayerGroupId, Guid UserId, IReadOnlyList<Guid> PersonIds) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<SetPayerGroupMembers>
    {
        public Validator()
        {
            RuleFor(x => x.PayerGroupId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.PersonIds).NotNull();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<SetPayerGroupMembers, Response>
    {
        public async Task<Response> Handle(SetPayerGroupMembers request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Setting members of payer group '{PayerGroupId}'", request.PayerGroupId);

            var groupExists = await context.PayerGroups
                .AnyAsync(g => g.Id == request.PayerGroupId && g.UserId == request.UserId, cancellationToken);
            if (!groupExists)
            {
                throw new NotFoundException<PayerGroup>($"Id: {request.PayerGroupId}");
            }

            var requestedIds = request.PersonIds.ToHashSet();
            if (requestedIds.Count > 0)
            {
                var existingIds = await context.People
                    .Where(p => requestedIds.Contains(p.Id) && p.UserId == request.UserId)
                    .Select(p => p.Id)
                    .ToHashSetAsync(cancellationToken);
                var missingIds = requestedIds.Except(existingIds).ToArray();
                if (missingIds.Length > 0)
                {
                    throw new NotFoundException<Person>($"PersonIds not found: {string.Join(", ", missingIds)}");
                }
            }

            var existingMembers = await context.PayerGroupMembers
                .Where(m => m.PayerGroupId == request.PayerGroupId)
                .ToListAsync(cancellationToken);

            // Removing someone who still holds a share of this group's payments would strand that
            // split outside the group, so block it rather than silently orphaning the data.
            var removedIds = existingMembers
                .Select(m => m.PersonId)
                .Where(id => !requestedIds.Contains(id))
                .ToHashSet();
            if (removedIds.Count > 0)
            {
                var stillSplit = await context.PaymentSplits
                    .Join(context.Payments, s => s.PaymentId, p => p.Id, (s, p) => new { s.PersonId, p.PayerGroupId })
                    .Where(x => x.PayerGroupId == request.PayerGroupId && removedIds.Contains(x.PersonId))
                    .Select(x => x.PersonId)
                    .Concat(context.EffectivePaymentSplits
                        .Join(context.Payments, s => s.PaymentId, p => p.Id, (s, p) => new { s.PersonId, p.PayerGroupId })
                        .Where(x => x.PayerGroupId == request.PayerGroupId && removedIds.Contains(x.PersonId))
                        .Select(x => x.PersonId))
                    .Distinct()
                    .ToArrayAsync(cancellationToken);

                if (stillSplit.Length > 0)
                {
                    throw new DomainValidationException([
                        new ValidationError
                        {
                            PropertyName = "PersonIds",
                            Errors = [$"These people still have a share of this group's payments: {string.Join(", ", stillSplit)}"]
                        }
                    ]);
                }
            }

            context.PayerGroupMembers.RemoveRange(existingMembers);
            foreach (var personId in requestedIds)
            {
                context.PayerGroupMembers.Add(new PayerGroupMember
                {
                    PayerGroupId = request.PayerGroupId,
                    PersonId = personId
                });
            }

            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Payer group '{PayerGroupId}' now has {Count} members", request.PayerGroupId, requestedIds.Count);

            return new Response(request.PayerGroupId, [.. requestedIds]);
        }
    }

    public record Response(Guid PayerGroupId, ICollection<Guid> PersonIds);
}
