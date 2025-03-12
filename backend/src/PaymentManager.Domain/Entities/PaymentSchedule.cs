using System;

namespace PaymentManager.Domain.Entities;

public record PaymentSchedule
{
    public required int Every { get; init; }
    public required Frequency Occurs { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public enum Frequency
    {
        Unknown,
        Once,
        Daily,
        Weekly,
        Monthly,
        Annually
    }
}
