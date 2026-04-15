namespace API.Application.Services;

using API.Application.Dtos.Reporting;
using API.Application.Interfaces;
using API.Domain.Entities;
using Microsoft.Extensions.Logging;

public class ReportingService : IReportingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        ILogger<ReportingService> logger)
    {
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ReportResponse> GenerateReportAsync(ReportRequestDto dto, int userId)
    {
        try
        {
            _logger.LogInformation("Generating {ReportType} report", dto.ReportType);

            var report = new ReportResponse
            {
                ReportId = new Random().Next(1000, 9999),
                ReportType = dto.ReportType,
                Format = dto.Format,
                Status = "READY",
                CreatedAt = DateTime.UtcNow,
                GeneratedAt = DateTime.UtcNow,
                GeneratedByUserId = userId,
                FileUrl = $"/api/v1/reports/{dto.ReportType.ToLower()}-{DateTime.UtcNow:yyyyMMdd}.{GetFileExtension(dto.Format)}"
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            throw;
        }
    }

    public async Task<ReportResponse> GetReportStatusAsync(int reportId)
    {
        return new ReportResponse
        {
            ReportId = reportId,
            Status = "READY",
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<byte[]> DownloadReportAsync(int reportId)
    {
        return System.Text.Encoding.UTF8.GetBytes("Report content placeholder");
    }

    public async Task<BookingReportDto> GetBookingReportAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var bookings = await _bookingRepository.GetAllAsync();
            var filtered = bookings
                .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
                .ToList();

            var report = new BookingReportDto
            {
                TotalBookings = filtered.Count,
                TotalRevenue = filtered.Sum(b => b.FinalAmount),
                ConfirmedBookings = filtered.Count(b => b.Status == 1),
                PendingBookings = filtered.Count(b => b.Status == 0),
                CancelledBookings = filtered.Count(b => b.Status == 3),
                AverageBookingValue = filtered.Count > 0 ? filtered.Average(b => b.FinalAmount) : 0,
                DailyMetrics = GenerateDailyBookingMetrics(filtered, startDate, endDate)
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating booking report");
            return new BookingReportDto();
        }
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var payments = await _paymentRepository.GetAllAsync();
            var filtered = payments
                .Where(p => p.Status == 1 && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToList();

            var report = new RevenueReportDto
            {
                TotalRevenue = filtered.Sum(p => p.Amount),
                AverageTransactionValue = filtered.Count > 0 ? filtered.Average(p => p.Amount) : 0,
                RevenueByPaymentMethod = filtered
                    .GroupBy(p => p.Provider)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount)),
                DailyMetrics = GenerateDailyRevenueMetrics(filtered, startDate, endDate)
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report");
            return new RevenueReportDto();
        }
    }

    public async Task<UserReportDto> GetUserReportAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var filtered = users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .ToList();

            var bookings = await _bookingRepository.GetAllAsync();

            var report = new UserReportDto
            {
                TotalUsers = users.Count(),
                ActiveUsers = users.Count(u => u.Status == 0),
                NewUsersThisPeriod = filtered.Count,
                UsersWithBookings = bookings.GroupBy(b => b.UserId).Count(),
                AverageSpendPerUser = CalculateAverageSpendPerUser(users.ToList(), bookings.ToList()),
                Segments = GenerateUserSegments(filtered)
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user report");
            return new UserReportDto();
        }
    }

    private List<DailyBookingMetric> GenerateDailyBookingMetrics(List<Booking> bookings, DateTime startDate, DateTime endDate)
    {
        var metrics = new List<DailyBookingMetric>();
        var current = startDate;

        while (current <= endDate)
        {
            var dayBookings = bookings.Where(b => b.CreatedAt.Date == current.Date).ToList();
            metrics.Add(new DailyBookingMetric
            {
                Date = current,
                Count = dayBookings.Count,
                Revenue = dayBookings.Sum(b => b.FinalAmount),
                AverageValue = dayBookings.Count > 0 ? dayBookings.Average(b => b.FinalAmount) : 0
            });
            current = current.AddDays(1);
        }

        return metrics;
    }

    private List<DailyRevenueMetric> GenerateDailyRevenueMetrics(List<Payment> payments, DateTime startDate, DateTime endDate)
    {
        var metrics = new List<DailyRevenueMetric>();
        var current = startDate;

        while (current <= endDate)
        {
            var dayPayments = payments.Where(p => p.CreatedAt.Date == current.Date).ToList();
            metrics.Add(new DailyRevenueMetric
            {
                Date = current,
                Amount = dayPayments.Sum(p => p.Amount),
                TransactionCount = dayPayments.Count
            });
            current = current.AddDays(1);
        }

        return metrics;
    }

    private decimal CalculateAverageSpendPerUser(List<User> users, List<Booking> bookings)
    {
        if (users.Count == 0)
            return 0;

        var totalSpend = bookings.Sum(b => b.FinalAmount);
        return totalSpend / users.Count;
    }

    private List<UserSegmentMetric> GenerateUserSegments(List<User> users)
    {
        return new List<UserSegmentMetric>
        {
            new() { Segment = "Premium", Count = users.Count(u => u.Email.Contains("premium")), AverageSpend = 1000 },
            new() { Segment = "Standard", Count = users.Count(u => !u.Email.Contains("premium")), AverageSpend = 500 },
            new() { Segment = "New", Count = users.Count(u => u.CreatedAt > DateTime.UtcNow.AddDays(-7)), AverageSpend = 300 }
        };
    }

    private string GetFileExtension(string format)
    {
        return format.ToLower() switch
        {
            "pdf" => "pdf",
            "excel" => "xlsx",
            "csv" => "csv",
            "json" => "json",
            _ => "pdf"
        };
    }
}
