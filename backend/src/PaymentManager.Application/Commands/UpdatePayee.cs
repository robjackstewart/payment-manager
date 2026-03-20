using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Common.Exceptions;
using static PaymentManager.Application.Commands.UpdatePayee;

namespace PaymentManager.Application.Commands;

public record UpdatePayee(Guid Id, Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<UpdatePayee>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdatePayee, Response>
    {
        public async Task<Response> Handle(UpdatePayee request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating payee '{Id}'", request.Id);
            var payee = await context.Payees.FindAsync([request.Id], cancellationToken);

            if (payee is null)
            {
                throw new NotFoundException<Payee>($"Id: {request.Id}");
            }

            payee = payee with
            {
                UserId = request.UserId,
                Name = request.Name
            };

            context.Payees.Update(payee);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Updated payee '{Id}' with name: '{Name}' for user: '{UserId}'", payee.Id, payee.Name, payee.UserId);

            return new Response(payee.Id, payee.UserId, payee.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
