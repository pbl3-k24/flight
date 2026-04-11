using Domain.Entities.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Infrastructure;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).HasColumnName("id");
        builder.Property(k => k.Key).HasColumnName("key").HasMaxLength(256).IsRequired();
        builder.Property(k => k.Operation).HasColumnName("operation").HasMaxLength(64).IsRequired();
        builder.Property(k => k.ResponseBody).HasColumnName("response_body").HasColumnType("jsonb");
        builder.Property(k => k.ResponseStatusCode).HasColumnName("response_status_code");
        builder.Property(k => k.ExpiresAt).HasColumnName("expires_at");
        builder.Property(k => k.UserId).HasColumnName("user_id");
        builder.Property(k => k.CreatedAt).HasColumnName("created_at");
        builder.Property(k => k.UpdatedAt).HasColumnName("updated_at");
        builder.Property(k => k.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(k => k.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(k => k.Key).IsUnique().HasDatabaseName("ix_idempotency_keys_key");
        builder.HasIndex(k => k.ExpiresAt).HasDatabaseName("ix_idempotency_keys_expires_at");
    }
}
