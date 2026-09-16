using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class PayerGroupEntityTypeConfiguration : IEntityTypeConfiguration<PayerGroup>
{
    public void Configure(EntityTypeBuilder<PayerGroup> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.UserId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
    }
}
