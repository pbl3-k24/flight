using Domain.Entities.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Notification;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.TemplateKey).HasColumnName("template_key").HasMaxLength(64).IsRequired();
        builder.Property(t => t.Subject).HasColumnName("subject").HasMaxLength(256).IsRequired();
        builder.Property(t => t.BodyHtml).HasColumnName("body_html").HasColumnType("text");
        builder.Property(t => t.BodyText).HasColumnName("body_text").HasColumnType("text");
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(t => t.TemplateKey).IsUnique().HasDatabaseName("ix_email_templates_key");
    }
}
