namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Email)
            .HasMaxLength(255);

        builder.Property(n => n.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(n => n.Content)
            .IsRequired();

        builder.Property(n => n.Status)
            .HasDefaultValue(0);

        builder.HasIndex(n => n.Status);

        builder.HasOne(n => n.User)
            .WithMany(u => u.NotificationLogs)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasCheckConstraint(
            "CK_NotificationLog_Status_Valid",
            "\"Status\" IN (0, 1, 2)");

        builder.HasCheckConstraint(
            "CK_NotificationLog_Email_Default",
            "\"Email\" IS NOT NULL OR \"UserId\" IS NOT NULL");

        builder.ToTable("NotificationLogs");
    }
}
