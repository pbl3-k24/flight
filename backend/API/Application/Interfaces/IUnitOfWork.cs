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
    IFlightScheduleTemplateRepository FlightScheduleTemplates { get; }
    IFlightTemplateDetailRepository FlightTemplateDetails { get; }
    IFlightDefinitionRepository FlightDefinitions { get; }

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

    /// <summary>
    /// Executes a transactional operation using the configured execution strategy.
    /// This is required when using retry execution strategies like NpgsqlRetryingExecutionStrategy.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);

    /// <summary>
    /// Executes a transactional operation using the configured execution strategy (void return).
    /// This is required when using retry execution strategies like NpgsqlRetryingExecutionStrategy.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation);
}
