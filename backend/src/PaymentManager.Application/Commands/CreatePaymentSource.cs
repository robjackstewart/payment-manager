using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreatePaymentSource;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.Application.Commands;

public record CreatePaymentSource(Guid UserId, string Name, string? Description) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreatePaymentSource>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal class Handler(IPaymentManagerContext Context, ILogger<Handler> Logger) : IRequestHandler<CreatePaymentSource, Response>
    {
        public async Task<Response> Handle(CreatePaymentSource request, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Creating new payment source for user {UserId}...", request.UserId);

            var userExists = await Context.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);

            if (!userExists)
            {
                Logger.LogWarning("User '{UserId}' not found.", request.UserId);
                throw new NotFoundException<User>($"Id is '{request.UserId}'");

            }

            var newPaymentSource = new PaymentSource
            {
                Name = request.Name,
                Description = request.Description,
                UserId = request.UserId
            };
            Context.PaymentSources.Add(newPaymentSource);
            await Context.SaveChanges(cancellationToken);

            Logger.LogInformation("New payment source '{Id}' created for user {UserId}.", newPaymentSource.Id, request.UserId);

            return new Response(newPaymentSource.Id, newPaymentSource.Name, newPaymentSource.Description);
        }
    }

    public record Response(Guid Id, string Name, string? Description);
}
