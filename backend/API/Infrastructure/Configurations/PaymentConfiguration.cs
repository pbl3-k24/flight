namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Method)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasDefaultValue(0);

        builder.Property(p => p.TransactionRef)
            .HasMaxLength(255);

        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.RefundRequest)
            .WithOne(rr => rr.Payment)
            .HasForeignKey<RefundRequest>(rr => rr.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Payments");
    }
}
