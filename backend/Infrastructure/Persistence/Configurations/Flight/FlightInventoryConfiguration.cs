using Domain.Entities.Flight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class FlightInventoryConfiguration : IEntityTypeConfiguration<FlightInventory>
{
    public void Configure(EntityTypeBuilder<FlightInventory> builder)
    {
        builder.ToTable("flight_inventories");

        builder.HasKey(fi => fi.Id);
        builder.Property(fi => fi.Id).HasColumnName("id");
        builder.Property(fi => fi.FlightId).HasColumnName("flight_id");
        builder.Property(fi => fi.FareClassId).HasColumnName("fare_class_id");
        builder.Property(fi => fi.TotalSeats).HasColumnName("total_seats");
        builder.Property(fi => fi.AvailableSeats).HasColumnName("available_seats");
        builder.Property(fi => fi.HeldSeats).HasColumnName("held_seats").HasDefaultValue(0);
        builder.Property(fi => fi.SoldSeats).HasColumnName("sold_seats").HasDefaultValue(0);
        builder.Property(fi => fi.CreatedAt).HasColumnName("created_at");
        builder.Property(fi => fi.UpdatedAt).HasColumnName("updated_at");
        builder.Property(fi => fi.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(fi => fi.DeletedAt).HasColumnName("deleted_at");

        // Unique: one inventory record per flight+class
        builder.HasIndex(fi => new { fi.FlightId, fi.FareClassId })
            .IsUnique()
            .HasDatabaseName("ix_flight_inventories_flight_fare");
        builder.HasIndex(fi => fi.FlightId).HasDatabaseName("ix_flight_inventories_flight_id");

        builder.HasOne(fi => fi.Flight)
            .WithMany(f => f.Inventories)
            .HasForeignKey(fi => fi.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fi => fi.FareClass)
            .WithMany(fc => fc.Inventories)
            .HasForeignKey(fi => fi.FareClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
