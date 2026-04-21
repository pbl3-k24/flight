namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AircraftConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Model)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.RegistrationNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.TotalSeats)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(a => a.RegistrationNumber).IsUnique();

        builder.HasMany(a => a.SeatTemplates)
            .WithOne(st => st.Aircraft)
            .HasForeignKey(st => st.AircraftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Flights)
            .WithOne(f => f.Aircraft)
            .HasForeignKey(f => f.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add CHECK constraints
        builder.HasCheckConstraint(
            "CK_Aircraft_TotalSeats_Positive",
            "\"TotalSeats\" > 0");

        builder.ToTable("Aircraft");
    }
}
