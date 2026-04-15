namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ServiceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.ServiceName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(b => b.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasOne(b => b.BookingPassenger)
            .WithMany(bp => bp.Services)
            .HasForeignKey(b => b.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("BookingServices");
    }
}
