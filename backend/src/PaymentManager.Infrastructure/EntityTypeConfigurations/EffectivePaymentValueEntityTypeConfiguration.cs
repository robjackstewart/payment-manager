using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class EffectivePaymentValueEntityTypeConfiguration : IEntityTypeConfiguration<EffectivePaymentValue>
{
    public void Configure(EntityTypeBuilder<EffectivePaymentValue> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PaymentId).IsRequired();
        builder.Property(x => x.EffectiveDate).IsRequired();
        builder.Property(x => x.Amount).IsRequired().HasPrecision(18, 2);
        builder.HasIndex(x => new { x.PaymentId, x.EffectiveDate }).IsUnique();
        builder.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
    }
}
