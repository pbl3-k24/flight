namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework configuration for account and auth entities.
/// </summary>
public class AuthAndAccountConfiguration :
    IEntityTypeConfiguration<Role>,
    IEntityTypeConfiguration<UserRole>,
    IEntityTypeConfiguration<EmailVerificationToken>,
    IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        // CreatedAt/UpdatedAt are shadow properties with SQL defaults in DbContext.
        builder.HasData(
            new Role { Id = 1, Name = "Admin", Description = "System administrator" },
            new Role { Id = 2, Name = "Staff", Description = "Operational staff" },
            new Role { Id = 3, Name = "Customer", Description = "Default customer role" }
        );
    }

    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserRoles_UserId");

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserRoles_RoleId");
    }

    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens", table =>
        {
            table.HasCheckConstraint(
                "CK_EmailVerificationTokens_UsedAfterIssue",
                "\"UsedAt\" IS NULL OR \"UsedAt\" <= \"ExpiresAt\"");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.HasOne(t => t.User)
            .WithMany(u => u.EmailVerificationTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_EmailVerificationTokens_UserId");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_EmailVerificationTokens_UserId");

        builder.HasIndex(t => t.Code)
            .HasDatabaseName("IX_EmailVerificationTokens_Code");
    }

    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens", table =>
        {
            table.HasCheckConstraint(
                "CK_PasswordResetTokens_UsedAfterIssue",
                "\"UsedAt\" IS NULL OR \"UsedAt\" <= \"ExpiresAt\"");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.HasOne(t => t.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PasswordResetTokens_UserId");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_PasswordResetTokens_UserId");

        builder.HasIndex(t => t.Code)
            .HasDatabaseName("IX_PasswordResetTokens_Code");
    }
}
