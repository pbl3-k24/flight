using Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Booking;

public class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.ToTable("passengers");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.BookingId).HasColumnName("booking_id");
        builder.Property(p => p.FullName).HasColumnName("full_name").HasMaxLength(256).IsRequired();
        builder.Property(p => p.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(p => p.Gender).HasColumnName("gender").HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.NationalId).HasColumnName("national_id").HasMaxLength(32);
        builder.Property(p => p.PassportNumber).HasColumnName("passport_number").HasMaxLength(32);
        builder.Property(p => p.PassportExpiry).HasColumnName("passport_expiry");
        builder.Property(p => p.Nationality).HasColumnName("nationality").HasMaxLength(64);
        builder.Property(p => p.PassengerType).HasColumnName("passenger_type").HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(p => p.BookingId).HasDatabaseName("ix_passengers_booking_id");

        builder.HasOne(p => p.Booking)
            .WithMany(b => b.Passengers)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
