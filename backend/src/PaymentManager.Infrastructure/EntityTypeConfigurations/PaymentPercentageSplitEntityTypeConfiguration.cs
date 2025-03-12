using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal sealed class PaymentPercentageSplitEntityTypeConfiguration : IEntityTypeConfiguration<PaymentPercentageSplit>
{
    public void Configure(EntityTypeBuilder<PaymentPercentageSplit> builder)
    {
        builder.HasKey(p => new { p.PaymentId, p.PaymentSourceId });
        builder.Property(p => p.Percentage).IsRequired();
        builder.HasOne(p => p.Payment).WithMany().HasForeignKey(p => p.PaymentId);
        builder.HasOne(p => p.PaymentSource).WithMany().HasForeignKey(p => p.PaymentSourceId);
    }
}
