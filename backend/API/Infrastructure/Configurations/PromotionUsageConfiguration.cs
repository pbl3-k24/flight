using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class PromotionUsageConfiguration : IEntityTypeConfiguration<PromotionUsage>
{
    public void Configure(EntityTypeBuilder<PromotionUsage> builder)
    {
        builder.ToTable("PromotionUsages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.UsedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasOne(x => x.Promotion)
            .WithMany(x => x.PromotionUsages)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.PromotionUsages)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PromotionId, x.BookingId }).IsUnique();
    }
}
