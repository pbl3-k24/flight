namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasDefaultValue(0);

        builder.Property(t => t.BookingId).IsRequired();
        builder.Property(t => t.PassengerId).IsRequired();
        builder.Property(t => t.FlightId).IsRequired();
        builder.Property(t => t.SeatClassId).IsRequired();
        builder.Property(t => t.Price).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
        
        builder.Property(t => t.SeatNumber).HasMaxLength(10);
        builder.Property(t => t.CheckInTime);
        builder.Property(t => t.BoardingTime);

        // Soft delete
        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAt);

        builder.HasIndex(t => t.TicketNumber).IsUnique();
        builder.HasIndex(t => t.Status);

        builder.HasOne(t => t.BookingPassenger)
            .WithMany(bp => bp.Tickets)
            .HasForeignKey(t => t.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.BookingPassengerId);

        builder.HasOne(t => t.ReplacedByTicket)
            .WithMany()
            .HasForeignKey(t => t.ReplacedByTicketId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.ReplacedByTicketId);

        builder.HasCheckConstraint(
            "CK_Ticket_Status_Valid",
            "\"Status\" IN (0, 1, 2, 3, 4, 5)");

        builder.ToTable("Tickets");
    }
}
