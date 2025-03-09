using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Queries.GetUser;

namespace PaymentManager.Application.Queries;

public record GetUser(Guid Id) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext readOnlyPaymentManagerContext) : IRequestHandler<GetUser, Response>
    {
        public async Task<Response> Handle(GetUser request, CancellationToken cancellationToken)
        {
            var user = await readOnlyPaymentManagerContext.Users.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (user is null)
            {
                throw new NotFoundException<User>($"{nameof(user.Id)} is '{request.Id}'");
            }
            return new Response(user.Id, user.Name);
        }
    }

    public record Response(Guid Id, string Name);
}
