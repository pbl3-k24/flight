using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.ToTable("Flights");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FlightNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasCheckConstraint("CK_Flight_DepartureBeforeArrival", "\"ArrivalTime\" > \"DepartureTime\"");
        builder.HasIndex(x => x.FlightNumber).IsUnique();
        builder.HasIndex(x => new { x.RouteId, x.DepartureTime });

        builder.HasOne(x => x.Route)
            .WithMany(x => x.Flights)
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Aircraft)
            .WithMany(x => x.Flights)
            .HasForeignKey(x => x.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
