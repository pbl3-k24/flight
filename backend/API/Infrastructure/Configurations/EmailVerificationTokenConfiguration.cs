namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(e => e.User)
            .WithMany(u => u.EmailVerificationTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasCheckConstraint(
            "CK_EmailVerificationToken_UsedAt_Valid",
            "\"UsedAt\" IS NULL OR \"UsedAt\" <= \"ExpiresAt\"");

        builder.ToTable("EmailVerificationTokens");
    }
}
