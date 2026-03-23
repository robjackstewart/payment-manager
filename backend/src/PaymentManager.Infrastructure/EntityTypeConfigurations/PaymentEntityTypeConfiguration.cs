using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class PaymentEntityTypeConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.PaymentSourceId).IsRequired();
        builder.Property(x => x.PayeeId).IsRequired();
        builder.Property(x => x.InitialAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Frequency).IsRequired().HasConversion<string>();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne<PaymentSource>().WithMany().HasForeignKey(x => x.PaymentSourceId);
        builder.HasOne<Payee>().WithMany().HasForeignKey(x => x.PayeeId);
    }
}
