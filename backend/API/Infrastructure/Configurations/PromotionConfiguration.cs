namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.DiscountType)
            .HasDefaultValue(0);

        builder.Property(p => p.DiscountValue)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.MaxDiscountAmount)
            .HasPrecision(10, 2);

        builder.Property(p => p.MinimumAmount)
            .HasPrecision(10, 2)
            .HasDefaultValue(0m);

        builder.Property(p => p.UsedCount)
            .HasDefaultValue(0);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        // Audit properties
        builder.Property(p => p.CreatedBy);

        builder.Property(p => p.UpdatedBy);

        builder.Property(p => p.UpdatedAt);

        // Soft delete
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt);

        // Concurrency token
        builder.Property(p => p.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.IsActive);

        builder.HasMany(p => p.PromotionUsages)
            .WithOne(pu => pu.Promotion)
            .HasForeignKey(pu => pu.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_Promotion_DiscountValue_Positive",
            "\"DiscountValue\" > 0");

        builder.HasCheckConstraint(
            "CK_Promotion_DiscountType_Valid",
            "\"DiscountType\" IN (0, 1)");

        builder.HasCheckConstraint(
            "CK_Promotion_Percentage_Max100",
            "\"DiscountType\" <> 0 OR \"DiscountValue\" <= 100");

        builder.HasCheckConstraint(
            "CK_Promotion_MinimumAmount_NonNegative",
            "\"MinimumAmount\" >= 0");

        builder.HasCheckConstraint(
            "CK_Promotion_ValidDateRange",
            "\"ValidFrom\" < \"ValidTo\"");

        builder.HasCheckConstraint(
            "CK_Promotion_UsedCount_NonNegative",
            "\"UsedCount\" >= 0");

        builder.ToTable("Promotions");
    }
}
