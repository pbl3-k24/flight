namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookingChangeRequestConfiguration : IEntityTypeConfiguration<BookingChangeRequest>
{
    public void Configure(EntityTypeBuilder<BookingChangeRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OldAmount).HasPrecision(10, 2);
        builder.Property(x => x.NewAmount).HasPrecision(10, 2);
        builder.Property(x => x.ChangeFee).HasPrecision(10, 2);
        builder.Property(x => x.NetAmount).HasPrecision(10, 2);
        builder.Property(x => x.CreditAmount).HasPrecision(10, 2);
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(0);

        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OldFlight)
            .WithMany()
            .HasForeignKey(x => x.OldFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NewFlight)
            .WithMany()
            .HasForeignKey(x => x.NewFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => new { x.BookingId, x.LegType, x.Status });

        builder.HasCheckConstraint(
            "CK_BookingChangeRequest_Status_Valid",
            "\"Status\" IN (0, 1, 2, 3, 4)");

        builder.ToTable("BookingChangeRequests");
    }
}

