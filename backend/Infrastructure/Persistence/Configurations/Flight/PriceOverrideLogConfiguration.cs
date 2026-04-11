using Domain.Entities.Flight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class PriceOverrideLogConfiguration : IEntityTypeConfiguration<PriceOverrideLog>
{
    public void Configure(EntityTypeBuilder<PriceOverrideLog> builder)
    {
        builder.ToTable("price_override_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.FlightFarePriceId).HasColumnName("flight_fare_price_id");
        builder.Property(l => l.AdminUserId).HasColumnName("admin_user_id");
        builder.Property(l => l.PriceBefore).HasColumnName("price_before").HasPrecision(18, 0);
        builder.Property(l => l.PriceAfter).HasColumnName("price_after").HasPrecision(18, 0);
        builder.Property(l => l.Reason).HasColumnName("reason").HasMaxLength(512);
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");
        builder.Property(l => l.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(l => l.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(l => l.FlightFarePriceId).HasDatabaseName("ix_price_override_logs_fare_price_id");
        builder.HasIndex(l => l.AdminUserId).HasDatabaseName("ix_price_override_logs_admin_user_id");

        builder.HasOne(l => l.FlightFarePrice)
            .WithMany(p => p.OverrideLogs)
            .HasForeignKey(l => l.FlightFarePriceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
