using System;
using Microsoft.EntityFrameworkCore;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Application.Common;

public interface IPaymentManagerContext
{
    public DbSet<User> Users { get; set; }

    public Task<int> SaveChanges(CancellationToken cancellationToken);
}
