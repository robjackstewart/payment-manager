using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Application.Common;

public interface IPaymentManagerContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentSource> PaymentSources { get; set; }
    public DbSet<PaymentPercentageSplit> PaymentPercentageSplits { get; set; }

    public DatabaseFacade Database { get; }

    public Task<int> SaveChanges(CancellationToken cancellationToken);
}
