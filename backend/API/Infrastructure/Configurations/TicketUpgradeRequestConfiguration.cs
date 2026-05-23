namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TicketUpgradeRequestConfiguration : IEntityTypeConfiguration<TicketUpgradeRequest>
{
    public void Configure(EntityTypeBuilder<TicketUpgradeRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PriceDifference).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.PaymentId).IsUnique();
        builder.HasIndex(x => new { x.TicketId, x.Status });
        builder.HasIndex(x => new { x.Status, x.ExpiresAt });

        builder.ToTable("TicketUpgradeRequests", t =>
        {
            t.HasCheckConstraint(
                "CK_TicketUpgradeRequest_Status_Valid",
                "\"Status\" IN (0, 1, 2, 3, 4)");
            t.HasCheckConstraint(
                "CK_TicketUpgradeRequest_PriceDifference_Positive",
                "\"PriceDifference\" > 0");
        });
    }
}
