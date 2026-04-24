namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Province)
            .HasMaxLength(100);

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);

        // Soft delete
        builder.Property(a => a.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(a => a.DeletedAt);

        builder.HasIndex(a => a.Code).IsUnique();

        builder.HasMany(a => a.DepartureRoutes)
            .WithOne(r => r.DepartureAirport)
            .HasForeignKey(r => r.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.ArrivalRoutes)
            .WithOne(r => r.ArrivalAirport)
            .HasForeignKey(r => r.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Airports");
    }
}
