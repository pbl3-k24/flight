namespace API.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IAirportRepository Airports { get; }
    IAircraftRepository Aircraft { get; }
    IRouteRepository Routes { get; }
    ISeatClassRepository SeatClasses { get; }
    IFlightRepository Flights { get; }
    IFlightSeatInventoryRepository FlightSeatInventories { get; }
    IBookingRepository Bookings { get; }
    IBookingPassengerRepository BookingPassengers { get; }
    IPaymentRepository Payments { get; }
    IRefundRequestRepository RefundRequests { get; }
    IPromotionRepository Promotions { get; }
    ITicketRepository Tickets { get; }
    INotificationLogRepository NotificationLogs { get; }
    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction for atomic operations.
    /// Must be paired with CommitAsync() or RollbackAsync().
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction, persisting all changes.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the current transaction, discarding all changes.
    /// </summary>
    Task RollbackAsync();
}
