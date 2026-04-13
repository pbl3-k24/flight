using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class RefundPolicyConfiguration : IEntityTypeConfiguration<RefundPolicy>
{
    public void Configure(EntityTypeBuilder<RefundPolicy> builder)
    {
        builder.ToTable("RefundPolicies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RefundPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.PenaltyFee).HasPrecision(18, 2).HasDefaultValue(0m);

        builder.HasCheckConstraint("CK_RefundPolicy_RefundPercent", "\"RefundPercent\" >= 0 AND \"RefundPercent\" <= 100");

        builder.HasOne(x => x.SeatClass)
            .WithMany(x => x.RefundPolicies)
            .HasForeignKey(x => x.SeatClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SeatClassId, x.HoursBeforeDeparture }).IsUnique();
    }
}
