using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal sealed class PaymentSourceEntityTypeConfiguration : IEntityTypeConfiguration<PaymentSource>
{
    public void Configure(EntityTypeBuilder<PaymentSource> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired();
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);
    }
}
