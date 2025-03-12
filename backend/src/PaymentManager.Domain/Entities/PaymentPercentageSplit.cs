using System;

namespace PaymentManager.Domain.Entities;

public sealed class PaymentPercentageSplit
{
    public required double Percentage { get; init; }
    public required Guid PaymentId { get; init; }
    public required Guid PaymentSourceId { get; init; }

    public PaymentSource PaymentSource { get; private set; }
    public Payment Payment { get; private set; }
}
