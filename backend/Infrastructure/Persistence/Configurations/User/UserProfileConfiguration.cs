using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.User;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.FullName).HasColumnName("full_name").HasMaxLength(256).IsRequired();
        builder.Property(p => p.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(p => p.Gender).HasColumnName("gender").HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Nationality).HasColumnName("nationality").HasMaxLength(64);
        builder.Property(p => p.NationalId).HasColumnName("national_id").HasMaxLength(32);
        builder.Property(p => p.PassportNumber).HasColumnName("passport_number").HasMaxLength(32);
        builder.Property(p => p.PassportExpiry).HasColumnName("passport_expiry");
        builder.Property(p => p.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(512);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("ix_user_profiles_user_id");
    }
}
