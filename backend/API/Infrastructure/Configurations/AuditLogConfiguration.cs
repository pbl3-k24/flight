using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasOne(x => x.Actor)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ActorId);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
