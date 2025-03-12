using System;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Application.Common;

public interface IReadOnlyPaymentManagerContext
{
    public IQueryable<User> Users { get; }
    public IQueryable<Payment> Payments { get; }
    public IQueryable<PaymentSource> PaymentSources { get; }
    public IQueryable<PaymentPercentageSplit> PaymentPercentageSplits { get; }
}
