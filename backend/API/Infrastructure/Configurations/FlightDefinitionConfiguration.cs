namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FlightDefinitionConfiguration : IEntityTypeConfiguration<FlightDefinition>
{
    public void Configure(EntityTypeBuilder<FlightDefinition> builder)
    {
        builder.ToTable("FlightDefinitions");

        builder.HasKey(fd => fd.Id);

        // Properties
        builder.Property(fd => fd.FlightNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(fd => fd.RouteId)
            .IsRequired();

        builder.Property(fd => fd.DefaultAircraftId)
            .IsRequired();

        builder.Property(fd => fd.DepartureTime)
            .IsRequired();

        builder.Property(fd => fd.ArrivalTime)
            .IsRequired();

        builder.Property(fd => fd.ArrivalOffsetDays)
            .HasDefaultValue(0);

        builder.Property(fd => fd.OperatingDays)
            .HasDefaultValue(127); // Every day

        builder.Property(fd => fd.IsActive)
            .HasDefaultValue(true);

        builder.Property(fd => fd.CreatedAt)
            .IsRequired();

        builder.Property(fd => fd.UpdatedAt);

        // Indexes
        builder.HasIndex(fd => fd.FlightNumber)
            .IsUnique();

        builder.HasIndex(fd => fd.RouteId);

        builder.HasIndex(fd => fd.IsActive);

        // Relationships
        builder.HasOne(fd => fd.Route)
            .WithMany()
            .HasForeignKey(fd => fd.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fd => fd.DefaultAircraft)
            .WithMany()
            .HasForeignKey(fd => fd.DefaultAircraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(fd => fd.Flights)
            .WithOne(f => f.FlightDefinition)
            .HasForeignKey(f => f.FlightDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
