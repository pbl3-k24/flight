using Domain.Entities.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Infrastructure;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        builder.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2048);
        builder.Property(e => e.AggregateId).HasColumnName("aggregate_id");
        builder.Property(e => e.AggregateName).HasColumnName("aggregate_name").HasMaxLength(128);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.Status).HasDatabaseName("ix_outbox_events_status");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_outbox_events_created_at");
        builder.HasIndex(e => new { e.AggregateId, e.AggregateName }).HasDatabaseName("ix_outbox_events_aggregate");
    }
}
