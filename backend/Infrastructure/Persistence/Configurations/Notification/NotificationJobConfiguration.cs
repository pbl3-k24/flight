using Domain.Entities.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Notification;

public class NotificationJobConfiguration : IEntityTypeConfiguration<NotificationJob>
{
    public void Configure(EntityTypeBuilder<NotificationJob> builder)
    {
        builder.ToTable("notification_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.JobType).HasColumnName("job_type").HasConversion<string>().HasMaxLength(64);
        builder.Property(j => j.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(j => j.RecipientEmail).HasColumnName("recipient_email").HasMaxLength(256);
        builder.Property(j => j.TemplateKey).HasColumnName("template_key").HasMaxLength(64);
        builder.Property(j => j.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(j => j.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(j => j.MaxRetries).HasColumnName("max_retries").HasDefaultValue(3);
        builder.Property(j => j.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(j => j.SentAt).HasColumnName("sent_at");
        builder.Property(j => j.ErrorMessage).HasColumnName("error_message").HasMaxLength(2048);
        builder.Property(j => j.RelatedBookingId).HasColumnName("related_booking_id");
        builder.Property(j => j.RelatedUserId).HasColumnName("related_user_id");
        builder.Property(j => j.CreatedAt).HasColumnName("created_at");
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at");
        builder.Property(j => j.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(j => j.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(j => j.Status).HasDatabaseName("ix_notification_jobs_status");
        builder.HasIndex(j => j.NextRetryAt).HasDatabaseName("ix_notification_jobs_next_retry_at");
        builder.HasIndex(j => j.RelatedBookingId).HasDatabaseName("ix_notification_jobs_booking_id");
    }
}
