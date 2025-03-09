using System;
using Microsoft.EntityFrameworkCore;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure;

internal sealed class ReadOnlyPaymentManagerContext(PaymentManagerContext context) : IReadOnlyPaymentManagerContext
{
    public IQueryable<User> Users => context.Users.AsNoTracking().AsQueryable();
}
