using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class PaymentSplitEntityTypeConfiguration : IEntityTypeConfiguration<PaymentSplit>
{
    public void Configure(EntityTypeBuilder<PaymentSplit> builder)
    {
        builder.HasKey(x => new { x.PaymentId, x.PersonId });
        builder.Property(x => x.Percentage).IsRequired().HasPrecision(5, 2);
        builder.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
