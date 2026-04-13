using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteEntity = API.Domain.Entities.Route;

namespace API.Infrastructure.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<RouteEntity>
{
    public void Configure(EntityTypeBuilder<RouteEntity> builder)
    {
        builder.ToTable("Routes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DistanceKm).IsRequired();
        builder.Property(x => x.EstimatedDurationMinutes).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasCheckConstraint("CK_Route_AirportsDifferent", "\"DepartureAirportId\" <> \"ArrivalAirportId\"");
        builder.HasCheckConstraint("CK_Route_Distance", "\"DistanceKm\" > 0");
        builder.HasCheckConstraint("CK_Route_Duration", "\"EstimatedDurationMinutes\" > 0");

        builder.HasOne(x => x.DepartureAirport)
            .WithMany(x => x.DepartureRoutes)
            .HasForeignKey(x => x.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ArrivalAirport)
            .WithMany(x => x.ArrivalRoutes)
            .HasForeignKey(x => x.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DepartureAirportId, x.ArrivalAirportId });
    }
}
