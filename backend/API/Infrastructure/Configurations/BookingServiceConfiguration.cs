using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> builder)
    {
        builder.ToTable("BookingServices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 2).IsRequired();

        builder.HasOne(x => x.BookingPassenger)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
