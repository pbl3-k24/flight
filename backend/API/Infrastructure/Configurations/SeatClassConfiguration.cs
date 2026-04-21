namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SeatClassConfiguration : IEntityTypeConfiguration<SeatClass>
{
    public void Configure(EntityTypeBuilder<SeatClass> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.RefundPercent)
            .HasPrecision(5, 2);

        builder.Property(s => s.ChangeFee)
            .HasPrecision(10, 2);

        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasMany(s => s.AircraftSeatTemplates)
            .WithOne(ast => ast.SeatClass)
            .HasForeignKey(ast => ast.SeatClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.FlightSeatInventories)
            .WithOne(fsi => fsi.SeatClass)
            .HasForeignKey(fsi => fsi.SeatClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.RefundPolicies)
            .WithOne(rp => rp.SeatClass)
            .HasForeignKey(rp => rp.SeatClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_SeatClass_RefundPercent_Valid",
            "\"RefundPercent\" >= 0 AND \"RefundPercent\" <= 100");

        builder.HasCheckConstraint(
            "CK_SeatClass_ChangeFee_NonNegative",
            "\"ChangeFee\" >= 0");

        builder.ToTable("SeatClasses");
    }
}
