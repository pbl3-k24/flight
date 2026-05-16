namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FlightScheduleTemplateConfiguration : IEntityTypeConfiguration<FlightScheduleTemplate>
{
    public void Configure(EntityTypeBuilder<FlightScheduleTemplate> builder)
    {
        builder.ToTable("FlightScheduleTemplates");
        
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.EffectiveFrom);

        builder.Property(t => t.EffectiveTo);
        
        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        
        builder.Property(t => t.UpdatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.Name);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_FlightScheduleTemplate_EffectiveRange",
            "\"EffectiveFrom\" IS NULL OR \"EffectiveTo\" IS NULL OR \"EffectiveFrom\" <= \"EffectiveTo\""));
    }
}
