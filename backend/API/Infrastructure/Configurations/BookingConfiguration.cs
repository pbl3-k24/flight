namespace API.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Domain.Entities;

/// <summary>
/// Entity Framework configuration for the Booking entity.
/// Defines the table structure, relationships, constraints, and indexes.
/// </summary>
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>
    /// Configures the Booking entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        // Table configuration
        builder.ToTable("Bookings");

        // Primary key
        builder.HasKey(b => b.Id);

        // Property configurations
        builder.Property(b => b.BookingReference)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Unique booking reference code (e.g., AA100-20260415-ABC123)");

        builder.Property(b => b.PassengerCount)
            .IsRequired()
            .HasComment("Number of passengers");

        builder.Property(b => b.TotalPrice)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasComment("Total booking price");

        builder.Property(b => b.Status)
            .IsRequired()
            .HasDefaultValue(API.Domain.Enums.BookingStatus.Pending)
            .HasComment("Booking status (Pending, Confirmed, CheckedIn, Cancelled)");

        builder.Property(b => b.Notes)
            .HasMaxLength(500)
            .HasComment("Optional notes or special requests");

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Creation timestamp");

        builder.Property(b => b.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Last update timestamp");

        builder.Property(b => b.CancelledAt)
            .HasComment("Cancellation timestamp (null if not cancelled)");

        // Foreign keys
        builder.HasOne(b => b.Flight)
            .WithMany(f => f.Bookings)
            .HasForeignKey(b => b.FlightId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Bookings_FlightId");

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Bookings_UserId");

        // Indexes
        builder.HasIndex(b => b.BookingReference)
            .IsUnique()
            .HasDatabaseName("IX_Bookings_BookingReference");

        builder.HasIndex(b => b.FlightId)
            .HasDatabaseName("IX_Bookings_FlightId");

        builder.HasIndex(b => b.UserId)
            .HasDatabaseName("IX_Bookings_UserId");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_Bookings_Status");

        builder.HasIndex(b => new { b.UserId, b.Status })
            .HasDatabaseName("IX_Bookings_UserId_Status");

        builder.HasIndex(b => b.CreatedAt)
            .HasDatabaseName("IX_Bookings_CreatedAt");

        // Relationships
        builder.HasMany<Passenger>()
            .WithOne(p => p.Booking)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Payment>()
            .WithOne()
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
