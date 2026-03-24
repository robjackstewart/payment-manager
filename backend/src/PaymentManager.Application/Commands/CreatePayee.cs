using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreatePayee;

namespace PaymentManager.Application.Commands;

public record CreatePayee(Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreatePayee>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreatePayee, Response>
    {
        public async Task<Response> Handle(CreatePayee request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating payee with name: '{Name}' for user '{UserId}'", request.Name, request.UserId);
            var payee = new Payee
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Name = request.Name
            };

            context.Payees.Add(payee);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created payee '{Id}' with name: '{Name}'", payee.Id, payee.Name);

            return new Response(payee.Id, payee.UserId, payee.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
