using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlightEntity = Domain.Entities.Flight.Flight;

namespace Infrastructure.Persistence.Configurations.Flight;

public class FlightConfiguration : IEntityTypeConfiguration<FlightEntity>
{
    public void Configure(EntityTypeBuilder<FlightEntity> builder)
    {
        builder.ToTable("flights");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.FlightNumber).HasColumnName("flight_number").HasMaxLength(16).IsRequired();
        builder.Property(f => f.RouteId).HasColumnName("route_id");
        builder.Property(f => f.AircraftId).HasColumnName("aircraft_id");
        builder.Property(f => f.DepartureTime).HasColumnName("departure_time");
        builder.Property(f => f.ArrivalTime).HasColumnName("arrival_time");
        builder.Property(f => f.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(f => f.GateNumber).HasColumnName("gate_number").HasMaxLength(16);
        builder.Property(f => f.Terminal).HasColumnName("terminal").HasMaxLength(16);
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");
        builder.Property(f => f.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");

        // Search index: departure date + route is the most common query
        builder.HasIndex(f => new { f.RouteId, f.DepartureTime }).HasDatabaseName("ix_flights_route_departure");
        builder.HasIndex(f => f.DepartureTime).HasDatabaseName("ix_flights_departure_time");
        builder.HasIndex(f => f.FlightNumber).HasDatabaseName("ix_flights_flight_number");
        builder.HasIndex(f => f.Status).HasDatabaseName("ix_flights_status");
        builder.HasIndex(f => f.IsDeleted).HasDatabaseName("ix_flights_is_deleted");

        builder.HasOne(f => f.Route)
            .WithMany(r => r.Flights)
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Aircraft)
            .WithMany(a => a.Flights)
            .HasForeignKey(f => f.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
