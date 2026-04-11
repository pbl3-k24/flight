using Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Payment;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.PaymentId).HasColumnName("payment_id");
        builder.Property(r => r.Amount).HasColumnName("amount").HasPrecision(18, 0);
        builder.Property(r => r.Reason).HasColumnName("reason").HasMaxLength(512);
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.GatewayRefundRef).HasColumnName("gateway_refund_ref").HasMaxLength(128);
        builder.Property(r => r.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(r => r.ProcessedAt).HasColumnName("processed_at");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(r => r.PaymentId).HasDatabaseName("ix_refunds_payment_id");
        builder.HasIndex(r => r.Status).HasDatabaseName("ix_refunds_status");

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
