using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.GoogleId).HasMaxLength(255);
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.GoogleId).IsUnique().HasFilter("\"GoogleId\" IS NOT NULL");
    }
}
