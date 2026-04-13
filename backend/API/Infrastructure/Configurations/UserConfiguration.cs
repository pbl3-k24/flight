namespace API.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Domain.Entities;

/// <summary>
/// Entity Framework configuration for the User entity.
/// Defines the table structure, relationships, constraints, and indexes.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Configures the User entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table configuration
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Property configurations
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Unique email address");

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("First name");

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Last name");

        builder.Property(u => u.DateOfBirth)
            .IsRequired()
            .HasComment("Date of birth");

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20)
            .HasComment("Optional phone number");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("Hashed password");

        builder.Property(u => u.Status)
            .IsRequired()
            .HasDefaultValue(API.Domain.Enums.UserStatus.Active)
            .HasComment("Account status (Active, Deactivated, Suspended)");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("TIMEZONE('UTC', NOW())")
            .HasComment("Creation timestamp");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("TIMEZONE('UTC', NOW())")
            .HasComment("Last update timestamp");

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("IX_Users_Status");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        // Relationships
        builder.HasMany(u => u.Bookings)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Payments)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
