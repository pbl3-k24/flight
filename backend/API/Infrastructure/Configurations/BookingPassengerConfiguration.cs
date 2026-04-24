namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
{
    public void Configure(EntityTypeBuilder<BookingPassenger> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.FullName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(b => b.Gender)
            .HasMaxLength(10);

        builder.Property(b => b.DateOfBirth);

        builder.Property(b => b.NationalId)
            .HasMaxLength(50);

        builder.Property(b => b.PassengerType)
            .HasDefaultValue(0);

        builder.HasOne(b => b.Booking)
            .WithMany(bo => bo.Passengers)
            .HasForeignKey(b => b.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.FlightSeatInventory)
            .WithMany(fsi => fsi.BookingPassengers)
            .HasForeignKey(b => b.FlightSeatInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Services)
            .WithOne(bs => bs.BookingPassenger)
            .HasForeignKey(bs => bs.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.FlightSeatInventoryId);

        builder.ToTable("BookingPassengers");
    }
}
