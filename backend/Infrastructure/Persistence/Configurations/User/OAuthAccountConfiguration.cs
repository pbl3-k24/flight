using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.User;

public class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("oauth_accounts");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.UserId).HasColumnName("user_id");
        builder.Property(o => o.Provider).HasColumnName("provider").HasConversion<string>().HasMaxLength(32);
        builder.Property(o => o.ProviderUserId).HasColumnName("provider_user_id").HasMaxLength(256);
        builder.Property(o => o.AccessToken).HasColumnName("access_token");
        builder.Property(o => o.RefreshToken).HasColumnName("refresh_token");
        builder.Property(o => o.TokenExpiry).HasColumnName("token_expiry");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(o => new { o.Provider, o.ProviderUserId }).IsUnique().HasDatabaseName("ix_oauth_accounts_provider_uid");
        builder.HasIndex(o => o.UserId).HasDatabaseName("ix_oauth_accounts_user_id");

        builder.HasOne(o => o.User)
            .WithMany(u => u.OAuthAccounts)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
