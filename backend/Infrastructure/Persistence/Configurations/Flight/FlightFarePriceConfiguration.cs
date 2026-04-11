using Domain.Entities.Flight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class FlightFarePriceConfiguration : IEntityTypeConfiguration<FlightFarePrice>
{
    public void Configure(EntityTypeBuilder<FlightFarePrice> builder)
    {
        builder.ToTable("flight_fare_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.FlightId).HasColumnName("flight_id");
        builder.Property(p => p.FareClassId).HasColumnName("fare_class_id");
        builder.Property(p => p.BasePrice).HasColumnName("base_price").HasPrecision(18, 0);
        builder.Property(p => p.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 0);
        builder.Property(p => p.FeeAmount).HasColumnName("fee_amount").HasPrecision(18, 0);
        builder.Property(p => p.TotalPrice).HasColumnName("total_price").HasPrecision(18, 0);
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(8).HasDefaultValue("VND");
        builder.Property(p => p.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(p => p.EffectiveTo).HasColumnName("effective_to");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(p => new { p.FlightId, p.FareClassId, p.EffectiveFrom })
            .HasDatabaseName("ix_flight_fare_prices_flight_fare_date");
        builder.HasIndex(p => p.FlightId).HasDatabaseName("ix_flight_fare_prices_flight_id");

        builder.HasOne(p => p.Flight)
            .WithMany(f => f.FarePrices)
            .HasForeignKey(p => p.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.FareClass)
            .WithMany(fc => fc.FarePrices)
            .HasForeignKey(p => p.FareClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
