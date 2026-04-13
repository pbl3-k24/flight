using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class FlightSeatInventoryConfiguration : IEntityTypeConfiguration<FlightSeatInventory>
{
    public void Configure(EntityTypeBuilder<FlightSeatInventory> builder)
    {
        builder.ToTable("FlightSeatInventories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BasePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CurrentPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Version).HasDefaultValue(0).IsConcurrencyToken();
        builder.Property(x => x.HeldSeats).HasDefaultValue(0);
        builder.Property(x => x.SoldSeats).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasCheckConstraint("CK_FlightSeatInventory_AvailableSeats", "\"AvailableSeats\" >= 0");
        builder.HasCheckConstraint("CK_FlightSeatInventory_HeldSeats", "\"HeldSeats\" >= 0");
        builder.HasCheckConstraint("CK_FlightSeatInventory_SoldSeats", "\"SoldSeats\" >= 0");
        builder.HasCheckConstraint("CK_FlightSeatInventory_TotalConsistency", "\"AvailableSeats\" + \"HeldSeats\" + \"SoldSeats\" <= \"TotalSeats\"");

        builder.HasOne(x => x.Flight)
            .WithMany(x => x.SeatInventories)
            .HasForeignKey(x => x.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SeatClass)
            .WithMany(x => x.FlightSeatInventories)
            .HasForeignKey(x => x.SeatClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FlightId, x.SeatClassId }).IsUnique();
    }
}
