namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.HasKey(f => f.Id);

        // Reference to FlightDefinition (replaces RouteId and AircraftId)
        builder.Property(f => f.FlightDefinitionId)
            .IsRequired();

        builder.Property(f => f.FlightNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(f => f.RouteId)
            .IsRequired();

        builder.Property(f => f.AircraftId)
            .IsRequired();

        builder.Property(f => f.ArrivalOffsetDays)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(f => f.DepartureTime)
            .IsRequired();

        builder.Property(f => f.ArrivalTime)
            .IsRequired();

        builder.Property(f => f.ActualAircraftId);

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

        // Indexes
        builder.HasIndex(f => f.FlightDefinitionId);
        builder.HasIndex(f => f.FlightNumber);
        builder.HasIndex(f => f.RouteId);
        builder.HasIndex(f => f.AircraftId);
        builder.HasIndex(f => f.DepartureTime);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => new { f.FlightDefinitionId, f.DepartureTime }).IsUnique();

        // Relationships
        builder.HasOne(f => f.FlightDefinition)
            .WithMany(fd => fd.Flights)
            .HasForeignKey(f => f.FlightDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Route)
            .WithMany()
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Aircraft)
            .WithMany()
            .HasForeignKey(f => f.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.ActualAircraft)
            .WithMany()
            .HasForeignKey(f => f.ActualAircraftId)
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
            "\"Status\" IN (0, 1, 2, 3, 4)");

        builder.HasCheckConstraint(
            "CK_Flight_ArrivalOffsetDays_NonNegative",
            "\"ArrivalOffsetDays\" >= 0");

        builder.ToTable("Flights");
    }
}

