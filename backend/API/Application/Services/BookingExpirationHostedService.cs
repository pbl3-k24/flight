namespace API.Application.Services;

using API.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class BookingExpirationHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingExpirationHostedService> _logger;

    public BookingExpirationHostedService(
        IServiceProvider serviceProvider,
        ILogger<BookingExpirationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking Expiration Hosted Service is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                    await backgroundJobService.ProcessExpiredBookingsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing booking expiration job.");
            }

            // Đợi 5 phút trước khi quét lại
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Booking Expiration Hosted Service is stopping.");
    }
}
