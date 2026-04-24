namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RefundPolicyConfiguration : IEntityTypeConfiguration<RefundPolicy>
{
    public void Configure(EntityTypeBuilder<RefundPolicy> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.HoursBeforeDeparture)
            .IsRequired();

        builder.Property(r => r.RefundPercent)
            .HasPrecision(5, 2);

        builder.Property(r => r.PenaltyFee)
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        // Soft delete
        builder.Property(r => r.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(r => r.DeletedAt);

        builder.HasOne(r => r.SeatClass)
            .WithMany(s => s.RefundPolicies)
            .HasForeignKey(r => r.SeatClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.SeatClassId);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_RefundPolicy_HoursBeforeDeparture_Positive",
            "\"HoursBeforeDeparture\" > 0");

        builder.HasCheckConstraint(
            "CK_RefundPolicy_RefundPercent_Valid",
            "\"RefundPercent\" >= 0 AND \"RefundPercent\" <= 100");

        builder.HasCheckConstraint(
            "CK_RefundPolicy_PenaltyFee_NonNegative",
            "\"PenaltyFee\" >= 0");

        builder.ToTable("RefundPolicies");
    }
}
