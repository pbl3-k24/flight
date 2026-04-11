using Domain.Entities.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Booking;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.TicketNumber).HasColumnName("ticket_number").HasMaxLength(64).IsRequired();
        builder.Property(t => t.BookingItemId).HasColumnName("booking_item_id");
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.IssuedAt).HasColumnName("issued_at");
        builder.Property(t => t.BarcodeData).HasColumnName("barcode_data").HasMaxLength(512);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(t => t.TicketNumber).IsUnique().HasDatabaseName("ix_tickets_ticket_number");
        builder.HasIndex(t => t.BookingItemId).IsUnique().HasDatabaseName("ix_tickets_booking_item_id");
        builder.HasIndex(t => t.Status).HasDatabaseName("ix_tickets_status");

        builder.HasOne(t => t.BookingItem)
            .WithOne(bi => bi.Ticket)
            .HasForeignKey<Ticket>(t => t.BookingItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
