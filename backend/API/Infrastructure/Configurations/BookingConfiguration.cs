namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(b => b.TripType)
            .HasDefaultValue(0);

        builder.Property(b => b.Status)
            .HasDefaultValue(0);

        builder.Property(b => b.ContactEmail)
            .IsRequired();

        builder.Property(b => b.ContactPhone)
            .HasMaxLength(20);

        builder.Property(b => b.TotalAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(b => b.DiscountAmount)
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        builder.Property(b => b.FinalAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(b => b.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND")
            .IsRequired();

        // Audit properties
        builder.Property(b => b.CreatedBy);

        builder.Property(b => b.UpdatedBy);

        // Soft delete
        builder.Property(b => b.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(b => b.DeletedAt);

        // Concurrency token
        builder.Property(b => b.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasIndex(b => b.BookingCode).IsUnique();
        builder.HasIndex(b => b.Status);

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.OutboundFlight)
            .WithMany(f => f.OutboundBookings)
            .HasForeignKey(b => b.OutboundFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ReturnFlight)
            .WithMany(f => f.ReturnBookings)
            .HasForeignKey(b => b.ReturnFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Passengers)
            .WithOne(bp => bp.Booking)
            .HasForeignKey(bp => bp.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Promotion)
            .WithMany()
            .HasForeignKey(b => b.PromotionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_Booking_TotalAmount_Positive",
            "\"TotalAmount\" > 0");

        builder.HasCheckConstraint(
            "CK_Booking_DiscountAmount_NonNegative",
            "\"DiscountAmount\" >= 0");

        builder.HasCheckConstraint(
            "CK_Booking_FinalAmount_Positive",
            "\"FinalAmount\" > 0");

        builder.HasCheckConstraint(
            "CK_Booking_FinalAmount_LessThanTotal",
            "\"FinalAmount\" <= \"TotalAmount\"");

        builder.HasCheckConstraint(
            "CK_Booking_TripType_Valid",
            "\"TripType\" IN (0, 1)");

        builder.HasCheckConstraint(
            "CK_Booking_Status_Valid",
            "\"Status\" IN (0, 1, 2, 3)");

        builder.HasCheckConstraint(
            "CK_Booking_ContactEmail_Default",
            "\"ContactEmail\" IS NOT NULL");

        builder.HasCheckConstraint(
            "CK_Booking_ContactPhone_Default",
            "\"ContactPhone\" IS NULL OR \"ContactPhone\" <> ''");

        builder.ToTable("Bookings");
    }
}
