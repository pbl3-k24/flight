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

        builder.HasIndex(t => t.TicketNumber).IsUnique();

        builder.HasOne(t => t.BookingPassenger)
            .WithOne(bp => bp.Ticket)
            .HasForeignKey<Ticket>(t => t.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.ReplacedByTicket)
            .WithMany()
            .HasForeignKey(t => t.ReplacedByTicketId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("Tickets");
    }
}
