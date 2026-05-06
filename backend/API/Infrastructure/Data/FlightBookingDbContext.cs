namespace API.Infrastructure.Data;

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set UpdatedAt for modified entities
        var modifiedEntries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            if (entry.Entity is not null)
            {
                // Update UpdatedAt for entities that have it
                var updatedAtProperty = entry.Entity.GetType().GetProperty("UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.SetValue(entry.Entity, DateTime.UtcNow);
                }

                // Increment Version for entities that have it
                var versionProperty = entry.Entity.GetType().GetProperty("Version");
                if (versionProperty != null && versionProperty.PropertyType == typeof(int))
                {
                    versionProperty.SetValue(entry.Entity, (int)versionProperty.GetValue(entry.Entity) + 1);
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
