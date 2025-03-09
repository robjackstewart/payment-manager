using System;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Application.Common;

public interface IReadOnlyPaymentManagerContext
{
    public IQueryable<User> Users { get; }
}
