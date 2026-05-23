namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingLegPassengerConfiguration : IEntityTypeConfiguration<BookingLegPassenger>
{
    public void Configure(EntityTypeBuilder<BookingLegPassenger> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentCheckStatus)
            .HasDefaultValue((int)PassengerDocumentCheckStatus.Pending);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasOne(x => x.BookingLeg)
            .WithMany(x => x.LegPassengers)
            .HasForeignKey(x => x.BookingLegId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BookingPassenger)
            .WithMany(x => x.LegPassengers)
            .HasForeignKey(x => x.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FlightSeatInventory)
            .WithMany()
            .HasForeignKey(x => x.FlightSeatInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BookingLegId);
        builder.HasIndex(x => x.BookingPassengerId);
        builder.HasIndex(x => new { x.BookingLegId, x.BookingPassengerId }).IsUnique();

        builder.ToTable("BookingLegPassengers");
    }
}

