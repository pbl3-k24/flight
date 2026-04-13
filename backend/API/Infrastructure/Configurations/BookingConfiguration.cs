using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.TripType).HasDefaultValue(0);
        builder.Property(x => x.ContactEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContactPhone).HasMaxLength(20);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.FinalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasCheckConstraint("CK_Booking_FinalAmount", "\"FinalAmount\" = \"TotalAmount\" - \"DiscountAmount\"");
        builder.HasCheckConstraint("CK_Booking_RoundTrip_ReturnFlight", "\"TripType\" <> 1 OR \"ReturnFlightId\" IS NOT NULL");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OutboundFlight)
            .WithMany(x => x.OutboundBookings)
            .HasForeignKey(x => x.OutboundFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReturnFlight)
            .WithMany(x => x.ReturnBookings)
            .HasForeignKey(x => x.ReturnFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Promotion)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.BookingCode).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
