using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class AircraftConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.ToTable("Aircraft");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Model).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RegistrationNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TotalSeats).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasCheckConstraint("CK_Aircraft_TotalSeats", "\"TotalSeats\" > 0");
        builder.HasIndex(x => x.RegistrationNumber).IsUnique();
    }
}
