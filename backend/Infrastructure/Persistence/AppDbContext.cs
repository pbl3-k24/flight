using FlightBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();
    public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

    // Flight
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Domain.Entities.Route> Routes => Set<Domain.Entities.Route>();
    public DbSet<Aircraft> Aircrafts => Set<Aircraft>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FareClass> FareClasses => Set<FareClass>();
    public DbSet<SeatTemplate> SeatTemplates => Set<SeatTemplate>();
    public DbSet<FlightInventory> FlightInventories => Set<FlightInventory>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<FlightFarePrice> FlightFarePrices => Set<FlightFarePrice>();
    public DbSet<PriceOverrideLog> PriceOverrideLogs => Set<PriceOverrideLog>();

    // Promotion
    public DbSet<Promotion> Promotions => Set<Promotion>();

    // Booking
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    // Payment
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<WalletLedger> WalletLedger => Set<WalletLedger>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    // Notifications
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<NotificationJob> NotificationJobs => Set<NotificationJob>();

    // System
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Auth ----
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Status).HasMaxLength(20);
            e.HasQueryFilter(u => u.DeletedAt == null);
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.User).WithOne(u => u.Profile)
             .HasForeignKey<UserProfile>(p => p.UserId);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            e.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        });

        modelBuilder.Entity<OAuthAccount>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => new { o.Provider, o.ProviderUserId }).IsUnique();
            e.HasOne(o => o.User).WithMany(u => u.OAuthAccounts).HasForeignKey(o => o.UserId);
        });

        modelBuilder.Entity<OtpToken>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasOne(o => o.User).WithMany(u => u.OtpTokens).HasForeignKey(o => o.UserId);
        });

        // ---- Flight ----
        modelBuilder.Entity<Airport>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Code).IsUnique();
        });

        modelBuilder.Entity<Domain.Entities.Route>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.OriginAirportId, r.DestinationAirportId }).IsUnique();
            e.HasOne(r => r.OriginAirport).WithMany(a => a.DepartureRoutes).HasForeignKey(r => r.OriginAirportId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.DestinationAirport).WithMany(a => a.ArrivalRoutes).HasForeignKey(r => r.DestinationAirportId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Aircraft>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Code).IsUnique();
        });

        modelBuilder.Entity<Flight>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.FlightNumber);
            e.HasIndex(f => new { f.RouteId, f.DepartureTime });
            e.HasOne(f => f.Route).WithMany(r => r.Flights).HasForeignKey(f => f.RouteId);
            e.HasOne(f => f.Aircraft).WithMany(a => a.Flights).HasForeignKey(f => f.AircraftId);
        });

        modelBuilder.Entity<FareClass>(e =>
        {
            e.HasKey(fc => fc.Id);
            e.HasIndex(fc => fc.Code).IsUnique();
            e.Property(fc => fc.RefundFeePercent).HasPrecision(5, 2);
            e.Property(fc => fc.ChangeFee).HasPrecision(18, 0);
        });

        modelBuilder.Entity<FlightInventory>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => new { i.FlightId, i.FareClassId }).IsUnique();
            e.HasOne(i => i.Flight).WithMany(f => f.Inventories).HasForeignKey(i => i.FlightId);
            e.HasOne(i => i.FareClass).WithMany(fc => fc.Inventories).HasForeignKey(i => i.FareClassId);
        });

        modelBuilder.Entity<FlightFarePrice>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.FlightId, p.FareClassId }).IsUnique();
            e.Property(p => p.CurrentPrice).HasPrecision(18, 0);
            e.HasOne(p => p.Flight).WithMany(f => f.FarePrices).HasForeignKey(p => p.FlightId);
            e.HasOne(p => p.FareClass).WithMany(fc => fc.FarePrices).HasForeignKey(p => p.FareClassId);
        });

        modelBuilder.Entity<PriceOverrideLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.PriceBefore).HasPrecision(18, 0);
            e.Property(l => l.PriceAfter).HasPrecision(18, 0);
            e.HasOne(l => l.FlightFarePrice).WithMany(p => p.OverrideLogs).HasForeignKey(l => l.FlightFarePriceId);
        });

        modelBuilder.Entity<PriceRule>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.BasePrice).HasPrecision(18, 0);
            e.Property(r => r.Multiplier).HasPrecision(5, 2);
            e.HasOne(r => r.Route).WithMany(rt => rt.PriceRules).HasForeignKey(r => r.RouteId).IsRequired(false);
        });

        // ---- Booking ----
        modelBuilder.Entity<Booking>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.BookingCode).IsUnique();
            e.HasIndex(b => b.UserId);
            e.Property(b => b.TotalAmount).HasPrecision(18, 0);
            e.HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId);
        });

        modelBuilder.Entity<Passenger>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.Booking).WithMany(b => b.Passengers).HasForeignKey(p => p.BookingId);
        });

        modelBuilder.Entity<BookingItem>(e =>
        {
            e.HasKey(bi => bi.Id);
            e.Property(bi => bi.Price).HasPrecision(18, 0);
            e.Property(bi => bi.TaxAndFee).HasPrecision(18, 0);
            e.HasOne(bi => bi.Booking).WithMany(b => b.Items).HasForeignKey(bi => bi.BookingId);
            e.HasOne(bi => bi.Passenger).WithMany(p => p.BookingItems).HasForeignKey(bi => bi.PassengerId);
            e.HasOne(bi => bi.Flight).WithMany(f => f.BookingItems).HasForeignKey(bi => bi.FlightId);
            e.HasOne(bi => bi.FareClass).WithMany(fc => fc.BookingItems).HasForeignKey(bi => bi.FareClassId);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.HasOne(t => t.BookingItem).WithOne(bi => bi.Ticket).HasForeignKey<Ticket>(t => t.BookingItemId);
        });

        // ---- Payment ----
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.TransactionRef).IsUnique();
            e.HasIndex(p => p.BookingId);
            e.Property(p => p.Amount).HasPrecision(18, 0);
            e.HasOne(p => p.Booking).WithMany(b => b.Payments).HasForeignKey(p => p.BookingId);
        });

        modelBuilder.Entity<PaymentEvent>(e =>
        {
            e.HasKey(pe => pe.Id);
            e.HasOne(pe => pe.Payment).WithMany(p => p.Events).HasForeignKey(pe => pe.PaymentId);
        });

        modelBuilder.Entity<Refund>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.PaymentId);
            e.Property(r => r.Amount).HasPrecision(18, 0);
            e.HasOne(r => r.Payment).WithMany(p => p.Refunds).HasForeignKey(r => r.PaymentId);
        });

        modelBuilder.Entity<WalletLedger>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Amount).HasPrecision(18, 0);
            e.Metadata.SetIsTableExcludedFromMigrations(false);
        });

        modelBuilder.Entity<IdempotencyKey>(e =>
        {
            e.HasKey(ik => ik.Id);
            e.HasIndex(ik => new { ik.Key, ik.OperationType }).IsUnique();
        });

        // ---- Promotion ----
        modelBuilder.Entity<Promotion>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.DiscountValue).HasPrecision(18, 0);
            e.Property(p => p.MinOrderAmount).HasPrecision(18, 0);
            e.Property(p => p.MaxDiscountAmount).HasPrecision(18, 0);
            e.Ignore(p => p.ApplicableRouteIds);
            e.Ignore(p => p.ApplicableFareClassIds);
        });

        // ---- Notifications ----
        modelBuilder.Entity<NotificationJob>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => j.Status);
        });

        // ---- System ----
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.EntityType);
            e.HasIndex(l => l.CreatedAt);
        });

        modelBuilder.Entity<OutboxEvent>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.IsProcessed);
        });

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "admin", Description = "System administrator" },
            new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "user", Description = "Regular user" }
        );
    }
}
