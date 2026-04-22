namespace API.Infrastructure.UnitOfWork;

using API.Application.Interfaces;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

public class UnitOfWork : IUnitOfWork
{
    private readonly FlightBookingDbContext _context;
    private readonly ILoggerFactory _loggerFactory;
    private IDbContextTransaction? _transaction;

    // Repository instances (lazy-loaded)
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IAirportRepository? _airports;
    private IAircraftRepository? _aircraft;
    private IRouteRepository? _routes;
    private ISeatClassRepository? _seatClasses;
    private IFlightRepository? _flights;
    private IFlightSeatInventoryRepository? _flightSeatInventories;
    private IBookingRepository? _bookings;
    private IBookingPassengerRepository? _bookingPassengers;
    private IPaymentRepository? _payments;
    private IRefundRequestRepository? _refundRequests;
    private IPromotionRepository? _promotions;
    private ITicketRepository? _tickets;
    private INotificationLogRepository? _notificationLogs;
    private IAuditLogRepository? _auditLogs;

    public UnitOfWork(FlightBookingDbContext context, ILoggerFactory loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    // Repository properties with lazy loading
    public IUserRepository Users => _users ??= new UserRepository(_context, _loggerFactory.CreateLogger<UserRepository>());
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context, _loggerFactory.CreateLogger<RoleRepository>());
    public IAirportRepository Airports => _airports ??= new AirportRepository(_context, _loggerFactory.CreateLogger<AirportRepository>());
    public IAircraftRepository Aircraft => _aircraft ??= new AircraftRepository(_context, _loggerFactory.CreateLogger<AircraftRepository>());
    public IRouteRepository Routes => _routes ??= new RouteRepository(_context, _loggerFactory.CreateLogger<RouteRepository>());
    public ISeatClassRepository SeatClasses => _seatClasses ??= new SeatClassRepository(_context, _loggerFactory.CreateLogger<SeatClassRepository>());
    public IFlightRepository Flights => _flights ??= new FlightRepository(_context, _loggerFactory.CreateLogger<FlightRepository>());
    public IFlightSeatInventoryRepository FlightSeatInventories => _flightSeatInventories ??= new FlightSeatInventoryRepository(_context, _loggerFactory.CreateLogger<FlightSeatInventoryRepository>());
    public IBookingRepository Bookings => _bookings ??= new BookingRepository(_context, _loggerFactory.CreateLogger<BookingRepository>());
    public IBookingPassengerRepository BookingPassengers => _bookingPassengers ??= new BookingPassengerRepository(_context, _loggerFactory.CreateLogger<BookingPassengerRepository>());
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context, _loggerFactory.CreateLogger<PaymentRepository>());
    public IRefundRequestRepository RefundRequests => _refundRequests ??= new RefundRequestRepository(_context, _loggerFactory.CreateLogger<RefundRequestRepository>());
    public IPromotionRepository Promotions => _promotions ??= new PromotionRepository(_context, _loggerFactory.CreateLogger<PromotionRepository>());
    public ITicketRepository Tickets => _tickets ??= new TicketRepository(_context, _loggerFactory.CreateLogger<TicketRepository>());
    public INotificationLogRepository NotificationLogs => _notificationLogs ??= new NotificationLogRepository(_context, _loggerFactory.CreateLogger<NotificationLogRepository>());
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context, _loggerFactory.CreateLogger<AuditLogRepository>());

    /// <summary>
    /// Begins a database transaction for atomic operations.
    /// Must be paired with CommitAsync() or RollbackAsync().
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
        _loggerFactory.CreateLogger<UnitOfWork>().LogInformation("Transaction started");
    }

    /// <summary>
    /// Commits the current transaction, persisting all changes.
    /// </summary>
    public async Task CommitAsync()
    {
        try
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit");
            }

            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
            _loggerFactory.CreateLogger<UnitOfWork>().LogInformation("Transaction committed successfully");
        }
        catch (Exception ex)
        {
            _loggerFactory.CreateLogger<UnitOfWork>().LogError(ex, "Error committing transaction");
            throw;
        }
    }

    /// <summary>
    /// Rolls back the current transaction, discarding all changes.
    /// </summary>
    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _loggerFactory.CreateLogger<UnitOfWork>().LogWarning("Transaction rolled back");
            }
        }
        catch (Exception ex)
        {
            _loggerFactory.CreateLogger<UnitOfWork>().LogError(ex, "Error rolling back transaction");
            throw;
        }
    }

    /// <summary>
    /// Saves changes without explicit transaction management.
    /// Used for non-transactional operations.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _loggerFactory.CreateLogger<UnitOfWork>().LogError(ex, "Error saving changes");
            throw;
        }
    }

    /// <summary>
    /// Disposes the transaction and context.
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }

        if (_context != null)
        {
            await _context.DisposeAsync();
        }
    }
}
