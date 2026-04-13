using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
{
    public void Configure(EntityTypeBuilder<BookingPassenger> builder)
    {
        builder.ToTable("BookingPassengers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Gender).HasMaxLength(10);
        builder.Property(x => x.NationalId).HasMaxLength(50);

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.Passengers)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FlightSeatInventory)
            .WithMany(x => x.BookingPassengers)
            .HasForeignKey(x => x.FlightSeatInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BookingId, x.FullName });
    }
}
