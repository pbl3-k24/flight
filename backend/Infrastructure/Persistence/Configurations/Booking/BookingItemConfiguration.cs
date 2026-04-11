using Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Booking;

public class BookingItemConfiguration : IEntityTypeConfiguration<BookingItem>
{
    public void Configure(EntityTypeBuilder<BookingItem> builder)
    {
        builder.ToTable("booking_items");

        builder.HasKey(bi => bi.Id);
        builder.Property(bi => bi.Id).HasColumnName("id");
        builder.Property(bi => bi.BookingId).HasColumnName("booking_id");
        builder.Property(bi => bi.PassengerId).HasColumnName("passenger_id");
        builder.Property(bi => bi.FlightId).HasColumnName("flight_id");
        builder.Property(bi => bi.FareClassId).HasColumnName("fare_class_id");
        builder.Property(bi => bi.SeatNumber).HasColumnName("seat_number").HasMaxLength(8);
        builder.Property(bi => bi.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 0);
        builder.Property(bi => bi.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 0);
        builder.Property(bi => bi.TotalPrice).HasColumnName("total_price").HasPrecision(18, 0);
        builder.Property(bi => bi.Currency).HasColumnName("currency").HasMaxLength(8).HasDefaultValue("VND");
        builder.Property(bi => bi.CreatedAt).HasColumnName("created_at");
        builder.Property(bi => bi.UpdatedAt).HasColumnName("updated_at");
        builder.Property(bi => bi.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(bi => bi.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(bi => bi.BookingId).HasDatabaseName("ix_booking_items_booking_id");
        builder.HasIndex(bi => bi.FlightId).HasDatabaseName("ix_booking_items_flight_id");
        builder.HasIndex(bi => bi.PassengerId).HasDatabaseName("ix_booking_items_passenger_id");

        builder.HasOne(bi => bi.Booking)
            .WithMany(b => b.Items)
            .HasForeignKey(bi => bi.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bi => bi.Passenger)
            .WithMany(p => p.BookingItems)
            .HasForeignKey(bi => bi.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bi => bi.Flight)
            .WithMany(f => f.BookingItems)
            .HasForeignKey(bi => bi.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bi => bi.FareClass)
            .WithMany(fc => fc.BookingItems)
            .HasForeignKey(bi => bi.FareClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
