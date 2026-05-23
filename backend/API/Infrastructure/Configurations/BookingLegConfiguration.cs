namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingLegConfiguration : IEntityTypeConfiguration<BookingLeg>
{
    public void Configure(EntityTypeBuilder<BookingLeg> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LegType)
            .HasDefaultValue(0);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasOne(x => x.Booking)
            .WithMany(b => b.Legs)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Flight)
            .WithMany()
            .HasForeignKey(x => x.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SeatClass)
            .WithMany()
            .HasForeignKey(x => x.SeatClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => new { x.BookingId, x.LegType }).IsUnique();

        builder.ToTable("BookingLegs");
    }
}

