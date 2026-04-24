namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.DistanceKm)
            .IsRequired();

        builder.Property(r => r.EstimatedDurationMinutes)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        // Soft delete
        builder.Property(r => r.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(r => r.DeletedAt);

        builder.HasOne(r => r.DepartureAirport)
            .WithMany(a => a.DepartureRoutes)
            .HasForeignKey(r => r.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ArrivalAirport)
            .WithMany(a => a.ArrivalRoutes)
            .HasForeignKey(r => r.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Flights)
            .WithOne(f => f.Route)
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_Route_DistanceKm_Positive",
            "\"DistanceKm\" > 0");

        builder.HasCheckConstraint(
            "CK_Route_EstimatedDurationMinutes_Positive",
            "\"EstimatedDurationMinutes\" > 0");

        builder.HasCheckConstraint(
            "CK_Route_DifferentAirports",
            "\"DepartureAirportId\" != \"ArrivalAirportId\"");

        builder.ToTable("Routes");
    }
}
