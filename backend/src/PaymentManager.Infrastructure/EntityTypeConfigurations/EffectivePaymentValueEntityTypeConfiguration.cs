using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class EffectivePaymentValueEntityTypeConfiguration : IEntityTypeConfiguration<EffectivePaymentValue>
{
    public void Configure(EntityTypeBuilder<EffectivePaymentValue> builder)
    {
        builder.HasKey(x => new { x.PaymentId, x.EffectiveDate });
        builder.Property(x => x.Amount).IsRequired().HasPrecision(18, 2);
        builder.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
    }
}
