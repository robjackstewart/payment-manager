using System;
using FluentValidation;

namespace PaymentManager.Application.Common.Validation;

public static class ValidationHandler<T>
{
    public static async Task ThrowIfInvalid(IValidator<T> validator, T model, CancellationToken cancellationToken)
        => await ThrowIfInvalid([validator], model, cancellationToken);
    public static async Task ThrowIfInvalid(IEnumerable<IValidator<T>> validators, T model, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<T>(model);

        var validationFailures = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .GroupBy(validationFailure => validationFailure.PropertyName)
            .Select(group => new ValidationError
            {
                PropertyName = group.Key,
                Errors = group.Select(validationFailure => validationFailure.ErrorMessage).ToArray()
            })
            .ToArray();

        if (errors.Length != 0)
        {
            throw new Exceptions.ValidationException(errors);
        }
    }
}
