using Domain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Payment;

public class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.ToTable("payment_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PaymentId).HasColumnName("payment_id");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(64);
        builder.Property(e => e.RawPayload).HasColumnName("raw_payload").HasColumnType("jsonb");
        builder.Property(e => e.GatewaySignature).HasColumnName("gateway_signature").HasMaxLength(512);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.PaymentId).HasDatabaseName("ix_payment_events_payment_id");

        builder.HasOne(e => e.Payment)
            .WithMany(p => p.Events)
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
