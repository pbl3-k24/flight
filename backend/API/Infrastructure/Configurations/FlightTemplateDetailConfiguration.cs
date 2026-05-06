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
        
        builder.Property(d => d.RouteId)
            .IsRequired();
        
        builder.Property(d => d.AircraftId)
            .IsRequired();
        
        builder.Property(d => d.DayOfWeek)
            .IsRequired();
        
        builder.Property(d => d.DepartureTime)
            .IsRequired();
        
        builder.Property(d => d.ArrivalTime)
            .IsRequired();
        
        builder.Property(d => d.FlightNumberPrefix)
            .IsRequired()
            .HasMaxLength(10);
        
        builder.Property(d => d.FlightNumberSuffix)
            .IsRequired()
            .HasMaxLength(10);
        
        builder.Property(d => d.CreatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(d => d.Template)
            .WithMany(t => t.Details)
            .HasForeignKey(d => d.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(d => d.Route)
            .WithMany()
            .HasForeignKey(d => d.RouteId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(d => d.Aircraft)
            .WithMany()
            .HasForeignKey(d => d.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(d => d.TemplateId);
        builder.HasIndex(d => d.RouteId);
        builder.HasIndex(d => d.AircraftId);
        builder.HasIndex(d => d.DayOfWeek);
        
        // Constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_FlightTemplateDetail_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6"));
    }
}
