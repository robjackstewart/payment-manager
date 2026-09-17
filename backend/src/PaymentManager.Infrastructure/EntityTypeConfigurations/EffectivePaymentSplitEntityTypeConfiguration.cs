using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class EffectivePaymentSplitEntityTypeConfiguration : IEntityTypeConfiguration<EffectivePaymentSplit>
{
    public void Configure(EntityTypeBuilder<EffectivePaymentSplit> builder)
    {
        builder.HasKey(x => new { x.PaymentId, x.EffectiveDate, x.PersonId });
        builder.Property(x => x.Percentage).IsRequired().HasPrecision(5, 2);
        builder.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
