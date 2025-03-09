using System;
using FluentValidation;
using MediatR;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using static PaymentManager.Application.Commands.CreateUser;

namespace PaymentManager.Application.Commands;

public record CreateUser(string Name) : IRequest<Response>
{
    internal sealed class Validator : AbstractValidator<CreateUser>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler(IPaymentManagerContext context) : IRequestHandler<CreateUser, Response>
    {
        private readonly IPaymentManagerContext _context = context;

        public async Task<Response> Handle(CreateUser request, CancellationToken cancellationToken)
        {
            var user = new User
            {
                Name = request.Name
            };

            _context.Users.Add(user);
            await _context.SaveChanges(cancellationToken);

            return new Response(user.Id, user.Name);
        }
    }

    public record Response(Guid Id, string Name);
}
