using Domain.Entities.Flight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Flight;

public class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        builder.ToTable("airports");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.IataCode).HasColumnName("iata_code").HasMaxLength(8).IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(a => a.City).HasColumnName("city").HasMaxLength(128).IsRequired();
        builder.Property(a => a.Country).HasColumnName("country").HasMaxLength(64).HasDefaultValue("Vietnam");
        builder.Property(a => a.TimeZone).HasColumnName("time_zone").HasMaxLength(64);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(a => a.IataCode).IsUnique().HasDatabaseName("ix_airports_iata_code");
        builder.HasIndex(a => a.City).HasDatabaseName("ix_airports_city");

        // Vietnamese domestic airport seed data
        builder.HasData(
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000001"), IataCode = "SGN", Name = "Sân bay Quốc tế Tân Sơn Nhất", City = "Hồ Chí Minh", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000002"), IataCode = "HAN", Name = "Sân bay Quốc tế Nội Bài", City = "Hà Nội", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000003"), IataCode = "DAD", Name = "Sân bay Quốc tế Đà Nẵng", City = "Đà Nẵng", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000004"), IataCode = "CXR", Name = "Sân bay Quốc tế Cam Ranh", City = "Nha Trang", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000005"), IataCode = "PQC", Name = "Sân bay Quốc tế Phú Quốc", City = "Phú Quốc", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000006"), IataCode = "HPH", Name = "Sân bay Quốc tế Cát Bi", City = "Hải Phòng", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000007"), IataCode = "HUI", Name = "Sân bay Quốc tế Phú Bài", City = "Huế", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000008"), IataCode = "UIH", Name = "Sân bay Phù Cát", City = "Quy Nhơn", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000009"), IataCode = "BMV", Name = "Sân bay Buôn Ma Thuột", City = "Buôn Ma Thuột", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000010"), IataCode = "DLI", Name = "Sân bay Quốc tế Liên Khương", City = "Đà Lạt", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000011"), IataCode = "VCL", Name = "Sân bay Chu Lai", City = "Tam Kỳ", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000012"), IataCode = "VDH", Name = "Sân bay Đồng Hới", City = "Đồng Hới", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000013"), IataCode = "VII", Name = "Sân bay Vinh", City = "Vinh", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000014"), IataCode = "VCA", Name = "Sân bay Quốc tế Cần Thơ", City = "Cần Thơ", TimeZone = "Asia/Ho_Chi_Minh" },
            new Airport { Id = new Guid("10000000-0000-0000-0000-000000000015"), IataCode = "VKG", Name = "Sân bay Rạch Giá", City = "Rạch Giá", TimeZone = "Asia/Ho_Chi_Minh" }
        );
    }
}
