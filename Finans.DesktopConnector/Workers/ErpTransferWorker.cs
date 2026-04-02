using Finans.Application.Abstractions.Transfer;

namespace Finans.DesktopConnector.Workers
{
    /// <summary>
    /// DesktopConnector'da çalışan ERP aktarım worker'ı.
    /// Pending durumundaki transfer item'larını Logo Tiger'a aktarır.
    /// </summary>
    public sealed class ErpTransferWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ErpTransferWorker> _logger;

        public ErpTransferWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ErpTransferWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _configuration.GetValue<int>("Worker:ErpTransferIntervalSeconds", 30);

            _logger.LogInformation("ERP Transfer Worker başlatıldı. Interval={Interval}s", intervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<IErpTransferExecutor>();

                    await executor.ExecutePendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERP transfer worker hatası.");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }
}
