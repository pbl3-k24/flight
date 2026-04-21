namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AircraftSeatTemplateConfiguration : IEntityTypeConfiguration<AircraftSeatTemplate>
{
    public void Configure(EntityTypeBuilder<AircraftSeatTemplate> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DefaultSeatCount)
            .IsRequired();

        builder.Property(a => a.DefaultBasePrice)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasOne(a => a.Aircraft)
            .WithMany(ac => ac.SeatTemplates)
            .HasForeignKey(a => a.AircraftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.SeatClass)
            .WithMany(s => s.AircraftSeatTemplates)
            .HasForeignKey(a => a.SeatClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.AircraftId, a.SeatClassId }).IsUnique();

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_AircraftSeatTemplate_DefaultSeatCount_Positive",
            "\"DefaultSeatCount\" > 0");

        builder.HasCheckConstraint(
            "CK_AircraftSeatTemplate_DefaultBasePrice_Positive",
            "\"DefaultBasePrice\" > 0");

        builder.ToTable("AircraftSeatTemplates");
    }
}
