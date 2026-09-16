using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using static PaymentManager.Application.Queries.GetAllPayerGroups;
using static PaymentManager.Application.Queries.GetAllPayerGroups.Response;

namespace PaymentManager.Application.Queries;

public record GetAllPayerGroups(Guid UserId) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllPayerGroups, Response>
    {
        public async Task<Response> Handle(GetAllPayerGroups request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all payer groups for user '{UserId}'...", request.UserId);

            var payerGroups = await context.PayerGroups
                .Where(x => x.UserId == request.UserId)
                .Select(x => new { x.Id, x.UserId, x.Name })
                .ToArrayAsync(cancellationToken);

            var groupIds = payerGroups.Select(g => g.Id).ToHashSet();
            var memberRows = await context.PayerGroupMembers
                .Where(m => groupIds.Contains(m.PayerGroupId))
                .Select(m => new { m.PayerGroupId, m.PersonId })
                .ToArrayAsync(cancellationToken);

            var membersByGroup = memberRows
                .GroupBy(m => m.PayerGroupId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.PersonId).ToArray());

            var dtos = payerGroups
                .Select(g => new PayerGroupDto(
                    g.Id,
                    g.UserId,
                    g.Name,
                    membersByGroup.GetValueOrDefault(g.Id) ?? []))
                .OrderBy(g => g.Name)
                .ToArray();

            logger.LogInformation("Successfully fetched {Count} payer groups for user '{UserId}'", dtos.Length, request.UserId);
            return new Response([.. dtos]);
        }
    }

    public record Response(ICollection<PayerGroupDto> PayerGroups)
    {
        public record PayerGroupDto(Guid Id, Guid UserId, string Name, ICollection<Guid> MemberPersonIds);
    }
}
