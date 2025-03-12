using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PaymentManager.Application.Common;
using PaymentManager.Domain.Entities;
using PaymentManager.Infrastructure.EntityTypeConfigurations;

namespace PaymentManager.Infrastructure;

internal class PaymentManagerContext(DbContextOptions<PaymentManagerContext> options) : DbContext(options), IPaymentManagerContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentSource> PaymentSources { get; set; }
    public DbSet<PaymentPercentageSplit> PaymentPercentageSplits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentSourceEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentPercentageSplitEntityTypeConfiguration());
    }

    public Task<int> SaveChanges(CancellationToken cancellationToken)
        => base.SaveChangesAsync(cancellationToken);

    internal class ContextDesignTimeFactory : IDesignTimeDbContextFactory<PaymentManagerContext>
    {
        public PaymentManagerContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PaymentManagerContext>();
            optionsBuilder.UseSqlite("Data Source=PaymentManager.DesignTime.db", b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName));
            return new PaymentManagerContext(optionsBuilder.Options);
        }
    }
}
