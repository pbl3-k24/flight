namespace API.Application.Services;

using API.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class VnpayRefundHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VnpayRefundHostedService> _logger;

    public VnpayRefundHostedService(
        IServiceProvider serviceProvider,
        ILogger<VnpayRefundHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VNPAY Refund Hosted Service is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                    await backgroundJobService.ProcessVnpayRefundQueueAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing VNPAY refunds.");
            }

            // Chờ 15 giây trước vòng lặp kế tiếp
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }

        _logger.LogInformation("VNPAY Refund Hosted Service is stopping.");
    }
}
