namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ClassServiceConfigConfiguration : IEntityTypeConfiguration<ClassServiceConfig>
{
    public void Configure(EntityTypeBuilder<ClassServiceConfig> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SeatClass)
            .WithMany()
            .HasForeignKey(x => x.SeatClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AdditionalService)
            .WithMany(x => x.ClassServiceConfigs)
            .HasForeignKey(x => x.AdditionalServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SeatClassId, x.AdditionalServiceId })
            .IsUnique();

        builder.ToTable("ClassServiceConfigs");
    }
}
