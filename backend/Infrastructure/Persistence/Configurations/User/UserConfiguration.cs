using Domain.Entities.User;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.User;

public class UserConfiguration : IEntityTypeConfiguration<Domain.Entities.User.User>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.User.User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash");
        builder.Property(u => u.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(u => u.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");

        // Unique constraint
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");
        builder.HasIndex(u => u.Phone).HasDatabaseName("ix_users_phone");
        builder.HasIndex(u => u.Status).HasDatabaseName("ix_users_status");
        builder.HasIndex(u => u.IsDeleted).HasDatabaseName("ix_users_is_deleted");

        // Relationships
        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
