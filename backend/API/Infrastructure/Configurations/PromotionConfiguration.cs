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

        builder.Property(p => p.DiscountType)
            .HasDefaultValue(0);

        builder.Property(p => p.DiscountValue)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.UsedCount)
            .HasDefaultValue(0);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(p => p.Code).IsUnique();

        builder.HasMany(p => p.PromotionUsages)
            .WithOne(pu => pu.Promotion)
            .HasForeignKey(pu => pu.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_Promotion_DiscountValue_Positive",
            "\"DiscountValue\" > 0");

        builder.HasCheckConstraint(
            "CK_Promotion_UsedCount_NonNegative",
            "\"UsedCount\" >= 0");

        builder.ToTable("Promotions");
    }
}
