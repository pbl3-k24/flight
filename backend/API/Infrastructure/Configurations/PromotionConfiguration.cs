using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DiscountValue).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.DiscountType).HasDefaultValue(0);
        builder.Property(x => x.UsedCount).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasCheckConstraint("CK_Promotion_ValidDateRange", "\"ValidTo\" > \"ValidFrom\"");
        builder.HasCheckConstraint("CK_Promotion_UsageLimit", "\"UsageLimit\" IS NULL OR \"UsedCount\" <= \"UsageLimit\"");

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
