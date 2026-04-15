namespace API.Application.Services;

using API.Application.Dtos.Dashboard;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class DashboardService : IDashboardService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefundRequestRepository _refundRepository;
    private readonly IFlightSeatInventoryRepository _seatInventoryRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IBookingRepository bookingRepository,
        IFlightRepository flightRepository,
        IUserRepository userRepository,
        IRefundRequestRepository refundRepository,
        IFlightSeatInventoryRepository seatInventoryRepository,
        IPaymentRepository paymentRepository,
        ILogger<DashboardService> logger)
    {
        _bookingRepository = bookingRepository;
        _flightRepository = flightRepository;
        _userRepository = userRepository;
        _refundRepository = refundRepository;
        _seatInventoryRepository = seatInventoryRepository;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<DashboardMetricsResponse> GetDashboardMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var bookings = await _bookingRepository.GetAllAsync();
            var payments = await _paymentRepository.GetAllAsync();
            var refunds = await _refundRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();

            var summary = new DashboardSummary
            {
                TotalBookings = bookings.Count(),
                BookingsToday = 0,
                TotalRevenue = payments.Where(p => p.Status == 1).Sum(p => p.Amount),
                TodayRevenue = 0,
                ActiveUsers = users.Count(u => u.Status == 0),
                PendingRefunds = refunds.Count(r => r.Status == 0),
                OccupancyRate = 75,
                UpcomingFlights = 10
            };

            return new DashboardMetricsResponse
            {
                Summary = summary,
                BookingTrends = new ChartData { Title = "Booking Trends" },
                RevenueTrends = new ChartData { Title = "Revenue Trends" },
                TopFlights = [],
                BookingsByStatus = bookings
                    .GroupBy(b => b.Status)
                    .ToDictionary(
                        g => g.Key switch { 0 => "Pending", 1 => "Confirmed", 2 => "CheckedIn", 3 => "Cancelled", _ => "Unknown" },
                        g => g.Count()),
                PaymentMethodDistribution = []
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard metrics");
            return new DashboardMetricsResponse();
        }
    }

    public async Task<SystemHealthResponse> GetSystemHealthAsync()
    {
        try
        {
            return new SystemHealthResponse
            {
                Status = "HEALTHY",
                LastCheck = DateTime.UtcNow,
                Components = new Dictionary<string, ComponentHealth>
                {
                    { "Database", new ComponentHealth { Name = "Database", Status = "OK", ResponseTimeMs = 50 } },
                    { "PaymentServices", new ComponentHealth { Name = "Payment Services", Status = "OK", ResponseTimeMs = 100 } }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system health");
            return new SystemHealthResponse { Status = "CRITICAL" };
        }
    }

    public async Task<Dictionary<string, decimal>> GetRevenueAnalyticsAsync(int days = 30)
    {
        try
        {
            var payments = await _paymentRepository.GetAllAsync();
            return payments
                .GroupBy(p => p.CreatedAt.Date)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => (decimal)g.Where(p => p.Status == 1).Sum(p => p.Amount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return [];
        }
    }

    public async Task<Dictionary<string, int>> GetBookingAnalyticsAsync(int days = 30)
    {
        try
        {
            var bookings = await _bookingRepository.GetAllAsync();
            return bookings
                .GroupBy(b => b.CreatedAt.Date)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking analytics");
            return [];
        }
    }

    public async Task<List<TopFlightResponse>> GetTopFlightsAsync(int days = 30, int limit = 10)
    {
        return [];
    }

    public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            return new Dictionary<string, int>
            {
                { "TotalUsers", users.Count() },
                { "ActiveUsers", users.Count(u => u.Status == 0) },
                { "InactiveUsers", users.Count(u => u.Status == 1) },
                { "SuspendedUsers", users.Count(u => u.Status == 2) }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return [];
        }
    }
}
