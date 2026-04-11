using Domain.Entities.Booking;
using Domain.Entities.Flight;
using Domain.Entities.Infrastructure;
using Domain.Entities.Notification;
using Domain.Entities.Payment;
using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── User group ──────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();

    // ── Flight group ─────────────────────────────────────────────────────────
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Aircraft> Aircrafts => Set<Aircraft>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FareClass> FareClasses => Set<FareClass>();
    public DbSet<FlightInventory> FlightInventories => Set<FlightInventory>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<FlightFarePrice> FlightFarePrices => Set<FlightFarePrice>();
    public DbSet<PriceOverrideLog> PriceOverrideLogs => Set<PriceOverrideLog>();

    // ── Booking group ─────────────────────────────────────────────────────────
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    // ── Payment group ─────────────────────────────────────────────────────────
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<WalletLedger> WalletLedger => Set<WalletLedger>();

    // ── Notification group ────────────────────────────────────────────────────
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<NotificationJob> NotificationJobs => Set<NotificationJob>();

    // ── Infrastructure group ──────────────────────────────────────────────────
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
