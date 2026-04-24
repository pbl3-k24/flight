namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Method)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasDefaultValue(0);

        builder.Property(p => p.TransactionRef)
            .HasMaxLength(255);

        // Audit properties
        builder.Property(p => p.CreatedBy);

        builder.Property(p => p.UpdatedBy);

        // Soft delete
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt);

        // Concurrency token
        builder.Property(p => p.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.RefundRequest)
            .WithOne(rr => rr.Payment)
            .HasForeignKey<RefundRequest>(rr => rr.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_Payment_Amount_Positive",
            "\"Amount\" > 0");

        builder.HasCheckConstraint(
            "CK_Payment_Status_Valid",
            "\"Status\" IN (0, 1, 2, 3)");

        builder.ToTable("Payments");
    }
}
