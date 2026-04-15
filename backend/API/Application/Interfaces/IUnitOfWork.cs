namespace API.Application.Interfaces;

public interface IUnitOfWork : IDisposable
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
}
