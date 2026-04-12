namespace API.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Domain.Entities;

/// <summary>
/// Entity Framework configuration for the Payment entity.
/// Defines the table structure, relationships, constraints, and indexes.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <summary>
    /// Configures the Payment entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Table configuration
        builder.ToTable("Payments");

        // Primary key
        builder.HasKey(p => p.Id);

        // Property configurations
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasComment("Payment amount");

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD")
            .HasComment("Currency code (e.g., USD, EUR)");

        builder.Property(p => p.Method)
            .IsRequired()
            .HasComment("Payment method (CreditCard, DebitCard, DigitalWallet, BankTransfer)");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasDefaultValue(API.Domain.Enums.PaymentStatus.Pending)
            .HasComment("Payment status (Pending, Processing, Completed, Failed, Refunded)");

        builder.Property(p => p.TransactionId)
            .HasMaxLength(100)
            .HasComment("External transaction ID from payment gateway");

        builder.Property(p => p.Notes)
            .HasMaxLength(500)
            .HasComment("Additional notes or error messages");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Creation timestamp");

        builder.Property(p => p.ProcessedAt)
            .HasComment("Processing/completion timestamp");

        // Foreign keys
        builder.HasOne(p => p.Booking)
            .WithOne()
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Payments_BookingId");

        builder.HasOne(p => p.User)
            .WithMany(u => u.Payments)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Payments_UserId");

        // Indexes
        builder.HasIndex(p => p.BookingId)
            .HasDatabaseName("IX_Payments_BookingId");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Payments_UserId");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Payments_Status");

        builder.HasIndex(p => p.TransactionId)
            .HasDatabaseName("IX_Payments_TransactionId");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");
    }
}
