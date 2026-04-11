using Domain.Entities.Flight;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class FareClassConfiguration : IEntityTypeConfiguration<FareClass>
{
    public void Configure(EntityTypeBuilder<FareClass> builder)
    {
        builder.ToTable("fare_classes");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.Code).HasColumnName("code").HasConversion<string>().HasMaxLength(32);
        builder.Property(f => f.DisplayName).HasColumnName("display_name").HasMaxLength(64);
        builder.Property(f => f.FreeBaggageKg).HasColumnName("free_baggage_kg");
        builder.Property(f => f.MealIncluded).HasColumnName("meal_included");
        builder.Property(f => f.RefundAllowed).HasColumnName("refund_allowed");
        builder.Property(f => f.RefundFeePercent).HasColumnName("refund_fee_percent").HasPrecision(5, 2);
        builder.Property(f => f.ChangeDateAllowed).HasColumnName("change_date_allowed");
        builder.Property(f => f.ChangeDateFeePercent).HasColumnName("change_date_fee_percent").HasPrecision(5, 2);
        builder.Property(f => f.Description).HasColumnName("description").HasMaxLength(512);
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");
        builder.Property(f => f.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(f => f.Code).IsUnique().HasDatabaseName("ix_fare_classes_code");

        // Seed standard fare classes
        builder.HasData(
            new FareClass
            {
                Id = new Guid("20000000-0000-0000-0000-000000000001"),
                Code = FareClassCode.Economy,
                DisplayName = "Phổ thông",
                FreeBaggageKg = 23,
                MealIncluded = false,
                RefundAllowed = true,
                RefundFeePercent = 30m,
                ChangeDateAllowed = true,
                ChangeDateFeePercent = 15m,
                Description = "Hạng phổ thông tiêu chuẩn"
            },
            new FareClass
            {
                Id = new Guid("20000000-0000-0000-0000-000000000002"),
                Code = FareClassCode.Business,
                DisplayName = "Thương gia",
                FreeBaggageKg = 40,
                MealIncluded = true,
                RefundAllowed = true,
                RefundFeePercent = 10m,
                ChangeDateAllowed = true,
                ChangeDateFeePercent = 5m,
                Description = "Hạng thương gia cao cấp"
            }
        );
    }
}
