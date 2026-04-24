namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(p => p.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasCheckConstraint(
            "CK_PasswordResetToken_UsedAt_Valid",
            "\"UsedAt\" IS NULL OR \"UsedAt\" <= \"ExpiresAt\"");

        builder.ToTable("PasswordResetTokens");
    }
}
