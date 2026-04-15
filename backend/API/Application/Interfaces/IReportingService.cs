namespace API.Application.Interfaces;

using API.Application.Dtos.Reporting;

public interface IReportingService
{
    /// <summary>
    /// Generates a report asynchronously.
    /// </summary>
    Task<ReportResponse> GenerateReportAsync(ReportRequestDto dto, int userId);

    /// <summary>
    /// Gets report status.
    /// </summary>
    Task<ReportResponse> GetReportStatusAsync(int reportId);

    /// <summary>
    /// Downloads a generated report.
    /// </summary>
    Task<byte[]> DownloadReportAsync(int reportId);

    /// <summary>
    /// Gets booking report.
    /// </summary>
    Task<BookingReportDto> GetBookingReportAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets revenue report.
    /// </summary>
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets user report.
    /// </summary>
    Task<UserReportDto> GetUserReportAsync(DateTime startDate, DateTime endDate);
}
