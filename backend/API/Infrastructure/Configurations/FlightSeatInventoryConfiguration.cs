namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FlightSeatInventoryConfiguration : IEntityTypeConfiguration<FlightSeatInventory>
{
    public void Configure(EntityTypeBuilder<FlightSeatInventory> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.TotalSeats)
            .IsRequired();

        builder.Property(f => f.AvailableSeats)
            .IsRequired();

        builder.Property(f => f.HeldSeats)
            .HasDefaultValue(0);

        builder.Property(f => f.SoldSeats)
            .HasDefaultValue(0);

        builder.Property(f => f.BasePrice)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(f => f.CurrentPrice)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(f => f.Version)
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        builder.HasOne(f => f.Flight)
            .WithMany(fl => fl.SeatInventories)
            .HasForeignKey(f => f.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.SeatClass)
            .WithMany(s => s.FlightSeatInventories)
            .HasForeignKey(f => f.SeatClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.BookingPassengers)
            .WithOne(bp => bp.FlightSeatInventory)
            .HasForeignKey(bp => bp.FlightSeatInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.FlightId, f.SeatClassId }).IsUnique();

        builder.ToTable("FlightSeatInventories");
    }
}
