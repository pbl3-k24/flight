namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FlightTemplateDetailConfiguration : IEntityTypeConfiguration<FlightTemplateDetail>
{
    public void Configure(EntityTypeBuilder<FlightTemplateDetail> builder)
    {
        builder.ToTable("FlightTemplateDetails");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.TemplateId)
            .IsRequired();
        
        builder.Property(d => d.FlightDefinitionId)
            .IsRequired();
        
        builder.Property(d => d.DayOfWeek)
            .IsRequired();
        
        builder.Property(d => d.AircraftOverrideId);

        builder.Property(d => d.DepartureTimeOverride);

        builder.Property(d => d.ArrivalTimeOverride);

        builder.Property(d => d.ArrivalOffsetDaysOverride);

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(d => d.CreatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(d => d.Template)
            .WithMany(t => t.Details)
            .HasForeignKey(d => d.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(d => d.FlightDefinition)
            .WithMany()
            .HasForeignKey(d => d.FlightDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.AircraftOverride)
            .WithMany()
            .HasForeignKey(d => d.AircraftOverrideId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(d => d.TemplateId);
        builder.HasIndex(d => d.FlightDefinitionId);
        builder.HasIndex(d => d.AircraftOverrideId);
        builder.HasIndex(d => d.DayOfWeek);
        builder.HasIndex(d => new { d.TemplateId, d.FlightDefinitionId, d.DayOfWeek }).IsUnique();
        
        // Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_FlightTemplateDetail_DayOfWeek", "\"DayOfWeek\" >= 0 AND \"DayOfWeek\" <= 6");
            t.HasCheckConstraint("CK_FlightTemplateDetail_ArrivalOffsetDaysOverride", "\"ArrivalOffsetDaysOverride\" IS NULL OR \"ArrivalOffsetDaysOverride\" >= 0");
        });
    }
}
