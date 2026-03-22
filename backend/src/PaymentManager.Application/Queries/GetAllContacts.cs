using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using static PaymentManager.Application.Queries.GetAllContacts;
using static PaymentManager.Application.Queries.GetAllContacts.Response;

namespace PaymentManager.Application.Queries;

public record GetAllContacts(Guid UserId) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<GetAllContacts, Response>
    {
        public async Task<Response> Handle(GetAllContacts request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching all contacts for user '{UserId}'...", request.UserId);
            var contacts = await context.Contacts
                .Where(x => x.UserId == request.UserId)
                .Select(x => new ContactDto(x.Id, x.UserId, x.Name))
                .ToArrayAsync(cancellationToken);
            logger.LogInformation("Successfully fetched {Count} contacts for user '{UserId}'", contacts.Length, request.UserId);
            return new Response([.. contacts.OrderBy(c => c.Name)]);
        }
    }

    public record Response(ICollection<ContactDto> Contacts)
    {
        public record ContactDto(Guid Id, Guid UserId, string Name);
    }
}
