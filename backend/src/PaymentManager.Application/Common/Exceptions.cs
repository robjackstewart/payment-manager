using PaymentManager.Application.Common.Validation;

namespace PaymentManager.Application.Common;

public static class Exceptions
{
    public sealed class ValidationException : Exception
    {
        public ValidationException(ICollection<ValidationError> errors)
        {
            Errors = errors;
        }

        public readonly ICollection<ValidationError> Errors;
    }

    public class NotFoundException(string entity, string criteria) : Exception($"{entity} not found with criteria: {criteria}")
    {
    }

    public sealed class NotFoundException<T>(string criteria) : NotFoundException($"{typeof(T).Name}", criteria)
    {
    }
}
