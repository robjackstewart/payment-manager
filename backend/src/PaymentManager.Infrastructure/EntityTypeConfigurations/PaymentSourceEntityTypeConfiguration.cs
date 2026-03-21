using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class PaymentSourceEntityTypeConfiguration : IEntityTypeConfiguration<PaymentSource>
{
    public void Configure(EntityTypeBuilder<PaymentSource> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
    }
}
