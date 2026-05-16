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

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasOne(p => p.Promotion)
            .WithMany(pr => pr.PromotionUsages)
            .HasForeignKey(p => p.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Booking)
            .WithMany()
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.PromotionId, p.UserId }).IsUnique();
        builder.HasIndex(p => p.BookingId).IsUnique();
        builder.HasIndex(p => p.PromotionId);
        builder.HasIndex(p => p.UserId);

        builder.ToTable("PromotionUsages");
    }
}
