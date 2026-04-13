namespace API.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using API.Domain.Entities;

/// <summary>
/// Entity Framework Core DbContext for the Flight Booking System.
/// Manages all database operations and entity relationships.
/// Implements the Unit of Work pattern through EF Core's change tracking.
/// </summary>
public class FlightBookingDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the Flights DbSet.
    /// </summary>
    public DbSet<Flight> Flights { get; set; }

    /// <summary>
    /// Gets or sets the Bookings DbSet.
    /// </summary>
    public DbSet<Booking> Bookings { get; set; }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Gets or sets the Roles DbSet.
    /// </summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    /// Gets or sets the UserRoles DbSet.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; }

    /// <summary>
    /// Gets or sets the EmailVerificationTokens DbSet.
    /// </summary>
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    /// <summary>
    /// Gets or sets the PasswordResetTokens DbSet.
    /// </summary>
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    /// <summary>
    /// Gets or sets the Payments DbSet.
    /// </summary>
    public DbSet<Payment> Payments { get; set; }

    /// <summary>
    /// Gets or sets the Passengers DbSet.
    /// </summary>
    public DbSet<Passenger> Passengers { get; set; }

    /// <summary>
    /// Gets or sets the Airports DbSet.
    /// </summary>
    public DbSet<Airport> Airports { get; set; }

    /// <summary>
    /// Gets or sets the CrewMembers DbSet.
    /// </summary>
    public DbSet<CrewMember> CrewMembers { get; set; }

    /// <summary>
    /// Gets or sets the FlightCrew junction entity DbSet.
    /// </summary>
    public DbSet<FlightCrew> FlightCrews { get; set; }

    /// <summary>
    /// Initializes a new instance of the FlightBookingDbContext class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public FlightBookingDbContext(DbContextOptions<FlightBookingDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the database model and entity relationships.
    /// Applies all entity configurations from the assembly.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base implementation
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlightBookingDbContext).Assembly);

        // Configure decimal properties with precision
        foreach (var property in modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Configure shadow properties for audit fields
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Only add shadow properties if the entity doesn't already have these properties
            if (!entityType.GetProperties().Any(p => p.Name == "CreatedAt"))
            {
                entityType.AddProperty("CreatedAt", typeof(DateTime)).SetDefaultValueSql("TIMEZONE('UTC', NOW())");
            }

            if (!entityType.GetProperties().Any(p => p.Name == "UpdatedAt"))
            {
                entityType.AddProperty("UpdatedAt", typeof(DateTime)).SetDefaultValueSql("TIMEZONE('UTC', NOW())");
            }
        }
    }

    /// <summary>
    /// Saves all changes to the database asynchronously.
    /// Updates ModifiedAt timestamps for modified entities before saving.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update ModifiedAt for all modified entities
        UpdateModifiedAt();

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes to the database synchronously.
    /// Updates ModifiedAt timestamps for modified entities before saving.
    /// Note: For async operations, use SaveChangesAsync instead.
    /// </summary>
    /// <returns>Number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        // Update ModifiedAt for all modified entities
        UpdateModifiedAt();

        return base.SaveChanges();
    }

    /// <summary>
    /// Internal method to update ModifiedAt timestamps for all modified entities.
    /// </summary>
    private void UpdateModifiedAt()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Flight flight)
            {
                flight.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Booking booking)
            {
                booking.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is User user)
            {
                user.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Payment payment)
            {
                payment.ProcessedAt ??= DateTime.UtcNow;
            }
            else if (entry.Entity is Passenger passenger)
            {
                // Passengers don't typically get UpdatedAt in this system
                // but you can add it if needed
            }
            else if (entry.Entity is Airport airport)
            {
                airport.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is CrewMember crewMember)
            {
                crewMember.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
