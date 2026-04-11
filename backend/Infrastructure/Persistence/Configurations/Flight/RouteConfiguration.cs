using Domain.Entities.Flight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.OriginAirportId).HasColumnName("origin_airport_id");
        builder.Property(r => r.DestinationAirportId).HasColumnName("destination_airport_id");
        builder.Property(r => r.IsDomestic).HasColumnName("is_domestic").HasDefaultValue(true);
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(r => new { r.OriginAirportId, r.DestinationAirportId })
            .IsUnique()
            .HasDatabaseName("ix_routes_origin_destination");
        builder.HasIndex(r => r.IsActive).HasDatabaseName("ix_routes_is_active");

        builder.HasOne(r => r.OriginAirport)
            .WithMany(a => a.OriginRoutes)
            .HasForeignKey(r => r.OriginAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.DestinationAirport)
            .WithMany(a => a.DestinationRoutes)
            .HasForeignKey(r => r.DestinationAirportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
