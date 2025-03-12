using FluentValidation;
using MediatR;

namespace PaymentManager.Application.Common.Validation;

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await ValidationHandler<TRequest>.ThrowIfInvalid(_validators, request, cancellationToken);
        var response = await next();
        return response;
    }
}
