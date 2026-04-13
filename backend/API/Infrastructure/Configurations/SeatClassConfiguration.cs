using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Configurations;

public class SeatClassConfiguration : IEntityTypeConfiguration<SeatClass>
{
    public void Configure(EntityTypeBuilder<SeatClass> builder)
    {
        builder.ToTable("SeatClasses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RefundPercent).HasPrecision(5, 2);
        builder.Property(x => x.ChangeFee).HasPrecision(18, 2);
        builder.Property(x => x.Priority).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new SeatClass { Id = 1, Code = "FIRST", Name = "First", RefundPercent = 100m, ChangeFee = 0m, Priority = 1 },
            new SeatClass { Id = 2, Code = "BUS", Name = "Business", RefundPercent = 100m, ChangeFee = 0m, Priority = 2 },
            new SeatClass { Id = 3, Code = "ECO", Name = "Economy", RefundPercent = 80m, ChangeFee = 50m, Priority = 3 });
    }
}
