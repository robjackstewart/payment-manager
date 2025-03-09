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

    public class NotFoundException(Type Type, string Criteria) : Exception($"{Type.Name} not found with criteria: {Criteria}")
    {
        public readonly Type Type = Type;
        public readonly string Criteria = Criteria;
    }

    public sealed class NotFoundException<T>(string Criteria) : NotFoundException(typeof(T), Criteria)
    {
    }
}
