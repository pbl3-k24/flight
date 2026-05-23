namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SavedPassengerConfiguration : IEntityTypeConfiguration<SavedPassenger>
{
    public void Configure(EntityTypeBuilder<SavedPassenger> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DateOfBirth)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Gender)
            .HasMaxLength(20);

        builder.Property(x => x.Nationality)
            .HasMaxLength(100);

        builder.Property(x => x.DocumentNumber)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        builder.Property(x => x.IsDefaultOwner)
            .HasDefaultValue(false);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsDefaultOwner })
            .HasFilter("\"IsDefaultOwner\" = true AND \"IsDeleted\" = false")
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.SavedPassengers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("SavedPassengers");
    }
}

