using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class PaymentSplitEntityTypeConfiguration : IEntityTypeConfiguration<PaymentSplit>
{
    public void Configure(EntityTypeBuilder<PaymentSplit> builder)
    {
        builder.HasKey(x => new { x.PaymentId, x.ContactId });
        builder.Property(x => x.Percentage).IsRequired().HasPrecision(5, 2);
        builder.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Contact>().WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
    }
}
