namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(b => b.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        // Soft delete
        builder.Property(b => b.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(b => b.DeletedAt);

        builder.HasOne(b => b.BookingPassenger)
            .WithMany(bp => bp.Services)
            .HasForeignKey(b => b.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.BookingLegPassenger)
            .WithMany(blp => blp.Services)
            .HasForeignKey(b => b.BookingLegPassengerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(b => b.BookingPassengerId);
        builder.HasIndex(b => b.BookingLegPassengerId);
        builder.HasIndex(b => new { b.BookingPassengerId, b.AdditionalServiceId });

        builder.HasOne(b => b.AdditionalService)
            .WithMany(a => a.BookingServices)
            .HasForeignKey(b => b.AdditionalServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("BookingServices");
    }
}
