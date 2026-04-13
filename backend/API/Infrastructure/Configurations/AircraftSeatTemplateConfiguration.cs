using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class AircraftSeatTemplateConfiguration : IEntityTypeConfiguration<AircraftSeatTemplate>
{
    public void Configure(EntityTypeBuilder<AircraftSeatTemplate> builder)
    {
        builder.ToTable("AircraftSeatTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DefaultSeatCount).IsRequired();
        builder.Property(x => x.DefaultBasePrice).HasPrecision(18, 2).IsRequired();

        builder.HasCheckConstraint("CK_AircraftSeatTemplate_DefaultSeatCount", "\"DefaultSeatCount\" > 0");
        builder.HasCheckConstraint("CK_AircraftSeatTemplate_DefaultBasePrice", "\"DefaultBasePrice\" > 0");

        builder.HasOne(x => x.Aircraft)
            .WithMany(x => x.SeatTemplates)
            .HasForeignKey(x => x.AircraftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SeatClass)
            .WithMany(x => x.AircraftSeatTemplates)
            .HasForeignKey(x => x.SeatClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AircraftId, x.SeatClassId }).IsUnique();
    }
}
