using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.User;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(256);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("ix_roles_name");

        // Seed data
        builder.HasData(
            new Role { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "Admin", Description = "System administrator" },
            new Role { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "Staff", Description = "Airline staff" },
            new Role { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "Customer", Description = "Regular customer" }
        );
    }
}
