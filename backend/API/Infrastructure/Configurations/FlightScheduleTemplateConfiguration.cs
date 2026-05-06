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
        
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.Description)
            .HasMaxLength(1000);
        
        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        
        builder.Property(t => t.UpdatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.Name);
    }
}
