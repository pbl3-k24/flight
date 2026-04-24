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

        // Audit properties
        builder.Property(r => r.CreatedBy);

        builder.Property(r => r.UpdatedBy);

        // Soft delete
        builder.Property(r => r.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(r => r.DeletedAt);

        // Concurrency token
        builder.Property(r => r.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasIndex(r => r.Status);

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

        builder.HasCheckConstraint(
            "CK_RefundRequest_Status_Valid",
            "\"Status\" IN (0, 1, 2, 3)");

        builder.HasCheckConstraint(
            "CK_RefundRequest_ProcessedAt_Valid",
            "(\"Status\" <> 2 AND \"ProcessedAt\" IS NULL) OR (\"Status\" = 2 AND \"ProcessedAt\" IS NOT NULL)");

        builder.ToTable("RefundRequests");
    }
}
