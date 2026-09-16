using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.UpdatePayerGroup;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record UpdatePayerGroup(Guid Id, Guid UserId, string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<UpdatePayerGroup>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    internal sealed class Handler(IPaymentManagerContext context, ILogger<Handler> logger) : IRequestHandler<UpdatePayerGroup, Response>
    {
        public async Task<Response> Handle(UpdatePayerGroup request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating payer group '{Id}'", request.Id);
            var payerGroup = await context.PayerGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

            if (payerGroup is null)
            {
                throw new NotFoundException<PayerGroup>($"Id: {request.Id}");
            }

            payerGroup = payerGroup with { UserId = request.UserId, Name = request.Name };

            context.PayerGroups.Update(payerGroup);
            await context.SaveChanges(cancellationToken);

            logger.LogInformation("Updated payer group '{Id}' with name: '{Name}'", payerGroup.Id, payerGroup.Name);

            return new Response(payerGroup.Id, payerGroup.UserId, payerGroup.Name);
        }
    }

    public record Response(Guid Id, Guid UserId, string Name);
}
