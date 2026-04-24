namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FlightNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.Status)
            .HasDefaultValue(0);

        // Audit properties
        builder.Property(f => f.CreatedBy);

        builder.Property(f => f.UpdatedBy);

        // Soft delete
        builder.Property(f => f.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(f => f.DeletedAt);

        // Concurrency token
        builder.Property(f => f.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasIndex(f => f.FlightNumber).IsUnique();
        builder.HasIndex(f => f.Status);

        builder.HasOne(f => f.Route)
            .WithMany(r => r.Flights)
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Aircraft)
            .WithMany(a => a.Flights)
            .HasForeignKey(f => f.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.SeatInventories)
            .WithOne(si => si.Flight)
            .HasForeignKey(si => si.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.OutboundBookings)
            .WithOne(b => b.OutboundFlight)
            .HasForeignKey(b => b.OutboundFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.ReturnBookings)
            .WithOne(b => b.ReturnFlight)
            .HasForeignKey(b => b.ReturnFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasCheckConstraint(
            "CK_Flight_Status_Valid",
            "\"Status\" IN (0, 1, 2, 3)");

        builder.ToTable("Flights");
    }
}
