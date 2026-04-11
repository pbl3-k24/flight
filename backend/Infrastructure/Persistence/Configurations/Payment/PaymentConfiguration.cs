using Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Payment;

public class PaymentConfiguration : IEntityTypeConfiguration<Domain.Entities.Payment.Payment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Payment.Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.BookingId).HasColumnName("booking_id");
        builder.Property(p => p.Gateway).HasColumnName("gateway").HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 0);
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(8).HasDefaultValue("VND");
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.TransactionRef).HasColumnName("transaction_ref").HasMaxLength(128);
        builder.Property(p => p.GatewayOrderId).HasColumnName("gateway_order_id").HasMaxLength(128);
        builder.Property(p => p.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        builder.Property(p => p.PaidAt).HasColumnName("paid_at");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(p => p.BookingId).HasDatabaseName("ix_payments_booking_id");
        builder.HasIndex(p => p.Status).HasDatabaseName("ix_payments_status");
        builder.HasIndex(p => p.TransactionRef).HasDatabaseName("ix_payments_transaction_ref");
        builder.HasIndex(p => p.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ix_payments_idempotency_key");

        builder.HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
