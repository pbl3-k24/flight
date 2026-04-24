namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PromotionUsageConfiguration : IEntityTypeConfiguration<PromotionUsage>
{
    public void Configure(EntityTypeBuilder<PromotionUsage> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DiscountAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasOne(p => p.Promotion)
            .WithMany(pr => pr.PromotionUsages)
            .HasForeignKey(p => p.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Booking)
            .WithMany()
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.PromotionId, p.BookingId }).IsUnique();
        builder.HasIndex(p => p.BookingId);

        builder.ToTable("PromotionUsages");
    }
}
