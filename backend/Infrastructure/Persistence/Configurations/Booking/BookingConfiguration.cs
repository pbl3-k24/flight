using Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Booking;

public class BookingConfiguration : IEntityTypeConfiguration<Domain.Entities.Booking.Booking>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Booking.Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.BookingCode).HasColumnName("booking_code").HasMaxLength(32).IsRequired();
        builder.Property(b => b.UserId).HasColumnName("user_id");
        builder.Property(b => b.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(b => b.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 0);
        builder.Property(b => b.Currency).HasColumnName("currency").HasMaxLength(8).HasDefaultValue("VND");
        builder.Property(b => b.ExpiresAt).HasColumnName("expires_at");
        builder.Property(b => b.ContactEmail).HasColumnName("contact_email").HasMaxLength(256);
        builder.Property(b => b.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(b => b.Notes).HasColumnName("notes").HasMaxLength(1024);
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(b => b.BookingCode).IsUnique().HasDatabaseName("ix_bookings_booking_code");
        builder.HasIndex(b => b.UserId).HasDatabaseName("ix_bookings_user_id");
        builder.HasIndex(b => b.Status).HasDatabaseName("ix_bookings_status");
        builder.HasIndex(b => b.ExpiresAt).HasDatabaseName("ix_bookings_expires_at");

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
