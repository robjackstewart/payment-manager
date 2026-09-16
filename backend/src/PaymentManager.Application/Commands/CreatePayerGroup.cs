using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreatePayerGroup;

namespace PaymentManager.Application.Commands;

public record CreatePayerGroup(Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreatePayerGroup>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<CreatePayerGroup, Response>
    {
        public async Task<Response> Handle(CreatePayerGroup request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating payer group with name: '{Name}' for user '{UserId}'", request.Name, request.UserId);
            var payerGroup = new PayerGroup
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Name = request.Name
            };

            context.PayerGroups.Add(payerGroup);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Created payer group '{Id}' with name: '{Name}'", payerGroup.Id, payerGroup.Name);

            return new Response(payerGroup.Id, payerGroup.UserId, payerGroup.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
