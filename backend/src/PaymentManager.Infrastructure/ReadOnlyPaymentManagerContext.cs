using Microsoft.EntityFrameworkCore;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure;

internal sealed class ReadOnlyPaymentManagerContext(IPaymentManagerContext context) : IReadOnlyPaymentManagerContext
{
    public IQueryable<User> Users => context.Users.AsNoTracking().AsQueryable();

    public IQueryable<Payment> Payments => context.Payments.AsNoTracking().AsQueryable();

    public IQueryable<PaymentSource> PaymentSources => context.PaymentSources.AsNoTracking().AsQueryable();

    public IQueryable<PaymentPercentageSplit> PaymentPercentageSplits => context.PaymentPercentageSplits.AsNoTracking().AsQueryable();
}
