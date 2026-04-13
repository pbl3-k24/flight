using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TicketNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.IssuedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        builder.HasOne(x => x.BookingPassenger)
            .WithOne(x => x.Ticket)
            .HasForeignKey<Ticket>(x => x.BookingPassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReplacedByTicket)
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTicketId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.TicketNumber).IsUnique();
        builder.HasIndex(x => x.BookingPassengerId).IsUnique();
    }
}
