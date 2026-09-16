using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure.EntityTypeConfigurations;

internal class PayerGroupMemberEntityTypeConfiguration : IEntityTypeConfiguration<PayerGroupMember>
{
    public void Configure(EntityTypeBuilder<PayerGroupMember> builder)
    {
        builder.HasKey(x => new { x.PayerGroupId, x.PersonId });
        builder.HasOne<PayerGroup>().WithMany().HasForeignKey(x => x.PayerGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Cascade);
    }
}
