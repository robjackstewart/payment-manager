using System;

namespace PaymentManager.Application.Common.Validation;

public record ValidationError
{
    public required string PropertyName { get; init; }
    public required ICollection<string> Errors { get; init; }
}
