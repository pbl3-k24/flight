namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RefundAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasMaxLength(500);

        builder.Property(r => r.Status)
            .HasDefaultValue(0);

        builder.HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Payment)
            .WithOne(p => p.RefundRequest)
            .HasForeignKey<RefundRequest>(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_RefundRequest_RefundAmount_Positive",
            "\"RefundAmount\" > 0");

        builder.ToTable("RefundRequests");
    }
}
