namespace API.Infrastructure.Data;

using API.Application.Common;
using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class FlightBookingDbContext : DbContext
{
    public FlightBookingDbContext(DbContextOptions<FlightBookingDbContext> options)
        : base(options)
    {
    }

    // User Management
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

    // Airport & Flight Management
    public DbSet<Airport> Airports { get; set; } = null!;
    public DbSet<Aircraft> Aircraft { get; set; } = null!;
    public DbSet<Route> Routes { get; set; } = null!;
    public DbSet<SeatClass> SeatClasses { get; set; } = null!;
    public DbSet<AircraftSeatTemplate> AircraftSeatTemplates { get; set; } = null!;
    public DbSet<FlightDefinition> FlightDefinitions { get; set; } = null!;
    public DbSet<Flight> Flights { get; set; } = null!;
    public DbSet<FlightSeatInventory> FlightSeatInventories { get; set; } = null!;

    // Booking Management
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<BookingPassenger> BookingPassengers { get; set; } = null!;
    public DbSet<BookingService> BookingServices { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<AdditionalService> AdditionalServices { get; set; } = null!;
    public DbSet<ClassServiceConfig> ClassServiceConfigs { get; set; } = null!;

    // Payment & Refund Management
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<RefundPolicy> RefundPolicies { get; set; } = null!;
    public DbSet<RefundRequest> RefundRequests { get; set; } = null!;

    // Promotion Management
    public DbSet<Promotion> Promotions { get; set; } = null!;
    public DbSet<PromotionUsage> PromotionUsages { get; set; } = null!;

    // Logging
    public DbSet<NotificationLog> NotificationLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    // Flight Schedule Templates
    public DbSet<FlightScheduleTemplate> FlightScheduleTemplates { get; set; } = null!;
    public DbSet<FlightTemplateDetail> FlightTemplateDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlightBookingDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyEntityAuditAndNormalizeTimes();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyEntityAuditAndNormalizeTimes();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyEntityAuditAndNormalizeTimes()
    {
        var trackedEntries = ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in trackedEntries)
        {
            if (entry.Entity is null)
            {
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var updatedAtProperty = entry.Entity.GetType().GetProperty("UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.SetValue(entry.Entity, DateTime.UtcNow);
                }

                var versionProperty = entry.Entity.GetType().GetProperty("Version");
                if (versionProperty != null && versionProperty.PropertyType == typeof(int))
                {
                    versionProperty.SetValue(entry.Entity, (int)(versionProperty.GetValue(entry.Entity) ?? 0) + 1);
                }
            }

            NormalizeDateTimePropertiesToUtc(entry);
        }
    }

    private static void NormalizeDateTimePropertiesToUtc(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.ClrType == typeof(DateTime))
            {
                if (property.CurrentValue is DateTime dateTimeValue)
                {
                    property.CurrentValue = VietnamTime.ToUtcFromVietnamStandard(dateTimeValue);
                }
                continue;
            }

            if (property.Metadata.ClrType == typeof(DateTime?))
            {
                if (property.CurrentValue is DateTime nullableDateTimeValue)
                {
                    property.CurrentValue = VietnamTime.ToUtcFromVietnamStandard(nullableDateTimeValue);
                }
            }
        }
    }
}
