namespace API.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Domain.Entities;

/// <summary>
/// Entity Framework configuration for the Flight entity.
/// Defines the table structure, relationships, constraints, and indexes.
/// </summary>
public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    /// <summary>
    /// Configures the Flight entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        // Table configuration
        builder.ToTable("Flights");

        // Primary key
        builder.HasKey(f => f.Id);

        // Property configurations
        builder.Property(f => f.FlightNumber)
            .IsRequired()
            .HasMaxLength(20)
            .HasComment("Flight designation (e.g., AA100)");

        builder.Property(f => f.Airline)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Airline name");

        builder.Property(f => f.AircraftModel)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Aircraft type");

        builder.Property(f => f.TotalSeats)
            .IsRequired()
            .HasComment("Total seat capacity");

        builder.Property(f => f.AvailableSeats)
            .IsRequired()
            .HasComment("Currently available seats");

        builder.Property(f => f.BasePrice)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasComment("Base price per seat");

        builder.Property(f => f.DepartureTime)
            .IsRequired()
            .HasComment("Scheduled departure time");

        builder.Property(f => f.ArrivalTime)
            .IsRequired()
            .HasComment("Scheduled arrival time");

        builder.Property(f => f.Status)
            .IsRequired()
            .HasDefaultValue(API.Domain.Enums.FlightStatus.Active)
            .HasComment("Flight status (Active, Cancelled, Delayed, Completed)");

        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Creation timestamp");

        builder.Property(f => f.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Last update timestamp");

        // Foreign keys
        builder.HasOne(f => f.DepartureAirport)
            .WithMany(a => a.DepartingFlights)
            .HasForeignKey(f => f.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Flights_DepartureAirportId");

        builder.HasOne(f => f.ArrivalAirport)
            .WithMany(a => a.ArrivingFlights)
            .HasForeignKey(f => f.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Flights_ArrivalAirportId");

        // Indexes
        builder.HasIndex(f => f.FlightNumber)
            .IsUnique()
            .HasDatabaseName("IX_Flights_FlightNumber");

        builder.HasIndex(f => f.DepartureAirportId)
            .HasDatabaseName("IX_Flights_DepartureAirportId");

        builder.HasIndex(f => f.ArrivalAirportId)
            .HasDatabaseName("IX_Flights_ArrivalAirportId");

        builder.HasIndex(f => f.Status)
            .HasDatabaseName("IX_Flights_Status");

        builder.HasIndex(f => f.DepartureTime)
            .HasDatabaseName("IX_Flights_DepartureTime");

        builder.HasIndex(f => new { f.DepartureAirportId, f.ArrivalAirportId, f.DepartureTime })
            .HasDatabaseName("IX_Flights_Route_DepartureTime");

        // Many-to-many relationship with CrewMembers through FlightCrew
        // Note: FlightCrew configuration is handled in CrewMemberAndFlightCrewConfiguration.cs

        // Relationships
        builder.HasMany(f => f.Bookings)
            .WithOne(b => b.Flight)
            .HasForeignKey(b => b.FlightId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
