using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RouteEntity = API.Domain.Entities.Route;

namespace API.Infrastructure.Data;

public class FlightBookingDbContext(DbContextOptions<FlightBookingDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Aircraft> Aircraft => Set<Aircraft>();
    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<SeatClass> SeatClasses => Set<SeatClass>();
    public DbSet<AircraftSeatTemplate> AircraftSeatTemplates => Set<AircraftSeatTemplate>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FlightSeatInventory> FlightSeatInventories => Set<FlightSeatInventory>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingPassenger> BookingPassengers => Set<BookingPassenger>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RefundPolicy> RefundPolicies => Set<RefundPolicy>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionUsage> PromotionUsages => Set<PromotionUsage>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlightBookingDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Modified))
        {
            var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
            if (updatedAtProperty is not null)
            {
                updatedAtProperty.CurrentValue = utcNow;
            }

            if (entry.Entity is FlightSeatInventory inventory)
            {
                inventory.Version++;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
