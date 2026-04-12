namespace API.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Domain.Entities;

/// <summary>
/// Entity Framework configuration for the CrewMember entity.
/// Defines the table structure, relationships, constraints, and indexes.
/// </summary>
public class CrewMemberConfiguration : IEntityTypeConfiguration<CrewMember>
{
    /// <summary>
    /// Configures the CrewMember entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<CrewMember> builder)
    {
        // Table configuration
        builder.ToTable("CrewMembers");

        // Primary key
        builder.HasKey(c => c.Id);

        // Property configurations
        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Crew member first name");

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Crew member last name");

        builder.Property(c => c.Role)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Job role (Pilot, Co-Pilot, Flight Attendant, etc.)");

        builder.Property(c => c.LicenseNumber)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("License/certification number");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Creation timestamp");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Last update timestamp");

        // Indexes
        builder.HasIndex(c => c.Role)
            .HasDatabaseName("IX_CrewMembers_Role");

        builder.HasIndex(c => c.LicenseNumber)
            .IsUnique()
            .HasDatabaseName("IX_CrewMembers_LicenseNumber");

        builder.HasIndex(c => new { c.FirstName, c.LastName })
            .HasDatabaseName("IX_CrewMembers_Name");
    }
}

/// <summary>
/// Entity Framework configuration for the FlightCrew junction entity.
/// Defines the many-to-many relationship between Flight and CrewMember.
/// </summary>
public class FlightCrewConfiguration : IEntityTypeConfiguration<FlightCrew>
{
    /// <summary>
    /// Configures the FlightCrew entity mapping.
    /// </summary>
    public void Configure(EntityTypeBuilder<FlightCrew> builder)
    {
        // Table configuration
        builder.ToTable("FlightCrews");

        // Primary key
        builder.HasKey(fc => fc.Id);

        // Property configurations
        builder.Property(fc => fc.AssignedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Assignment timestamp");

        // Foreign keys
        builder.HasOne(fc => fc.Flight)
            .WithMany(f => f.CrewMembers)
            .HasForeignKey(fc => fc.FlightId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FlightCrews_FlightId");

        builder.HasOne(fc => fc.CrewMember)
            .WithMany(c => c.FlightAssignments)
            .HasForeignKey(fc => fc.CrewMemberId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FlightCrews_CrewMemberId");

        // Indexes
        builder.HasIndex(fc => fc.FlightId)
            .HasDatabaseName("IX_FlightCrews_FlightId");

        builder.HasIndex(fc => fc.CrewMemberId)
            .HasDatabaseName("IX_FlightCrews_CrewMemberId");

        builder.HasIndex(fc => new { fc.FlightId, fc.CrewMemberId })
            .IsUnique()
            .HasDatabaseName("IX_FlightCrews_FlightId_CrewMemberId");

        // Unique constraint: A crew member can only be assigned once per flight
        builder.HasAlternateKey(fc => new { fc.FlightId, fc.CrewMemberId })
            .HasName("AK_FlightCrews_FlightId_CrewMemberId");
    }
}
