namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.GoogleId)
            .HasMaxLength(255);

        builder.Property(u => u.Status)
            .HasDefaultValue(0);

        // Security properties
        builder.Property(u => u.IsEmailVerified)
            .HasDefaultValue(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);

        builder.Property(u => u.PasswordChangedAt);

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.IsTwoFactorEnabled)
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorSecret)
            .HasMaxLength(500);

        builder.Property(u => u.PhoneNumberVerified)
            .HasDefaultValue(false);

        // Profile properties
        builder.Property(u => u.DateOfBirth)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.Nationality)
            .HasMaxLength(100);

        builder.Property(u => u.PassportExpiryDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.PassportCountry)
            .HasMaxLength(100);

        builder.Property(u => u.Gender)
            .HasMaxLength(10);

        builder.Property(u => u.MaritalStatus)
            .HasMaxLength(50);

        builder.Property(u => u.Occupation)
            .HasMaxLength(255);

        builder.Property(u => u.Address)
            .HasMaxLength(500);

        builder.Property(u => u.City)
            .HasMaxLength(100);

        builder.Property(u => u.Country)
            .HasMaxLength(100);

        builder.Property(u => u.ZipCode)
            .HasMaxLength(20);

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(10);

        builder.Property(u => u.PreferredCurrency)
            .HasMaxLength(10);

        builder.Property(u => u.TimeZone)
            .HasMaxLength(100);

        builder.Property(u => u.MarketingOptIn)
            .HasDefaultValue(false);

        builder.Property(u => u.NewsletterSubscription)
            .HasDefaultValue(false);

        builder.Property(u => u.NotificationPreferences)
            .HasColumnType("jsonb");

        // Audit properties
        builder.Property(u => u.CreatedBy);

        builder.Property(u => u.UpdatedBy);

        // Soft delete
        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt);

        // Concurrency token
        builder.Property(u => u.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.GoogleId).IsUnique();
        builder.HasIndex(u => u.Status);

        builder.HasMany(u => u.Bookings)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.EmailVerificationTokens)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.PasswordResetTokens)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.NotificationLogs)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.AuditLogs)
            .WithOne(al => al.Actor)
            .HasForeignKey(al => al.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("Users");
    }
}
