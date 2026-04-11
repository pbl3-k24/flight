using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.User;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).HasColumnName("id");
        builder.Property(ur => ur.UserId).HasColumnName("user_id");
        builder.Property(ur => ur.RoleId).HasColumnName("role_id");
        builder.Property(ur => ur.CreatedAt).HasColumnName("created_at");
        builder.Property(ur => ur.UpdatedAt).HasColumnName("updated_at");
        builder.Property(ur => ur.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(ur => ur.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasDatabaseName("ix_user_roles_user_role");

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
