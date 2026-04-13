using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.ToTable("RefundRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RefundAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.RefundRequests)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Payment)
            .WithOne(x => x.RefundRequest)
            .HasForeignKey<RefundRequest>(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PaymentId).IsUnique();
        builder.HasIndex(x => x.BookingId);
    }
}
