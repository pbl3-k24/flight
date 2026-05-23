namespace API.Infrastructure.Configurations;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FlightDisruptionDecisionConfiguration : IEntityTypeConfiguration<FlightDisruptionDecision>
{
    public void Configure(EntityTypeBuilder<FlightDisruptionDecision> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LegType).HasDefaultValue(0);
        builder.Property(x => x.Status).HasDefaultValue(0);
        builder.Property(x => x.DecisionType).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.Version).HasDefaultValue(0).IsConcurrencyToken();
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => new { x.BookingId, x.AffectedFlightId, x.LegType })
            .IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.DecisionDeadline);

        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AffectedFlight)
            .WithMany()
            .HasForeignKey(x => x.AffectedFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NewFlight)
            .WithMany()
            .HasForeignKey(x => x.NewFlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("FlightDisruptionDecisions");
    }
}
