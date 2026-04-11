using Domain.Entities.Flight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class PriceRuleConfiguration : IEntityTypeConfiguration<PriceRule>
{
    public void Configure(EntityTypeBuilder<PriceRule> builder)
    {
        builder.ToTable("price_rules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.RouteId).HasColumnName("route_id");
        builder.Property(r => r.BasePrice).HasColumnName("base_price").HasPrecision(18, 0);
        builder.Property(r => r.Multiplier).HasColumnName("multiplier").HasPrecision(5, 4);
        builder.Property(r => r.DayOfWeek).HasColumnName("day_of_week");
        builder.Property(r => r.SeasonMonth).HasColumnName("season_month");
        builder.Property(r => r.DaysBeforeDeparture).HasColumnName("days_before_departure");
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(r => r.Priority).HasColumnName("priority").HasDefaultValue(0);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(r => new { r.RouteId, r.IsActive, r.Priority }).HasDatabaseName("ix_price_rules_route_active_priority");

        builder.HasOne(r => r.Route)
            .WithMany(rt => rt.PriceRules)
            .HasForeignKey(r => r.RouteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
