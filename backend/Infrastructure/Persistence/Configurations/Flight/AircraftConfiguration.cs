using Domain.Entities.Flight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class AircraftConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.ToTable("aircrafts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.RegistrationCode).HasColumnName("registration_code").HasMaxLength(16).IsRequired();
        builder.Property(a => a.Model).HasColumnName("model").HasMaxLength(64).IsRequired();
        builder.Property(a => a.TotalSeats).HasColumnName("total_seats");
        builder.Property(a => a.EconomySeats).HasColumnName("economy_seats");
        builder.Property(a => a.BusinessSeats).HasColumnName("business_seats");
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(a => a.RegistrationCode).IsUnique().HasDatabaseName("ix_aircrafts_registration_code");
    }
}
