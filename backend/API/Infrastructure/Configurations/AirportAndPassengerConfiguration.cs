namespace API.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Domain.Entities;

/// <summary>
/// Entity Framework configuration for the Airport entity.
/// Defines the table structure, relationships, constraints, and indexes.
/// </summary>
public class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    /// <summary>
    /// Configures the Airport entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        // Table configuration
        builder.ToTable("Airports");

        // Primary key
        builder.HasKey(a => a.Id);

        // Property configurations
        builder.Property(a => a.Code)
            .IsRequired()
            .HasMaxLength(3)
            .HasComment("IATA airport code (e.g., LAX, JFK)");

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Full airport name");

        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("City location");

        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Country location");

        builder.Property(a => a.Timezone)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Airport timezone (e.g., America/Los_Angeles)");

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("TIMEZONE('UTC', NOW())")
            .HasComment("Creation timestamp");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("TIMEZONE('UTC', NOW())")
            .HasComment("Last update timestamp");

        // Indexes
        builder.HasIndex(a => a.Code)
            .IsUnique()
            .HasDatabaseName("IX_Airports_Code");

        builder.HasIndex(a => a.City)
            .HasDatabaseName("IX_Airports_City");

        builder.HasIndex(a => a.Country)
            .HasDatabaseName("IX_Airports_Country");

        // Relationships
        builder.HasMany(a => a.DepartingFlights)
            .WithOne(f => f.DepartureAirport)
            .HasForeignKey(f => f.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.ArrivingFlights)
            .WithOne(f => f.ArrivalAirport)
            .HasForeignKey(f => f.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity Framework configuration for the Passenger entity.
/// Defines the table structure, relationships, constraints, and indexes.
/// </summary>
public class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    /// <summary>
    /// Configures the Passenger entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        // Table configuration
        builder.ToTable("Passengers");

        // Primary key
        builder.HasKey(p => p.Id);

        // Property configurations
        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Passenger first name");

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Passenger last name");

        builder.Property(p => p.DateOfBirth)
            .IsRequired()
            .HasComment("Passenger date of birth");

        builder.Property(p => p.PassportNumber)
            .HasMaxLength(50)
            .HasComment("Optional passport number");

        builder.Property(p => p.Nationality)
            .HasMaxLength(3)
            .HasComment("Optional nationality code (ISO 3166-1)");

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Passenger email address");

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(20)
            .HasComment("Optional phone number");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("TIMEZONE('UTC', NOW())")
            .HasComment("Creation timestamp");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("TIMEZONE('UTC', NOW())")
            .HasComment("Last update timestamp");

        // Foreign keys
        builder.HasOne(p => p.Booking)
            .WithMany(b => b.Passengers)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Passengers_BookingId");

        // Indexes
        builder.HasIndex(p => p.BookingId)
            .HasDatabaseName("IX_Passengers_BookingId");

        builder.HasIndex(p => p.Email)
            .HasDatabaseName("IX_Passengers_Email");

        builder.HasIndex(p => new { p.FirstName, p.LastName })
            .HasDatabaseName("IX_Passengers_Name");
    }
}
