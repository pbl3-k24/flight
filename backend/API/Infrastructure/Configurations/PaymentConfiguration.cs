using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Method).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.TransactionRef).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasOne(x => x.Booking)
            .WithOne(x => x.Payment)
            .HasForeignKey<Payment>(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BookingId).IsUnique();
        builder.HasIndex(x => x.TransactionRef);
    }
}
