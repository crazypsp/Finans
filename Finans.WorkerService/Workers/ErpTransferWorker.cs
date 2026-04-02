using Finans.Application.Abstractions.Transfer;

namespace Finans.WorkerService.Workers
{
    public sealed class ErpTransferWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ErpTransferWorker> _logger;
        private readonly IConfiguration _configuration;

        public ErpTransferWorker(
            IServiceProvider serviceProvider,
            ILogger<ErpTransferWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _configuration.GetValue<int>("Worker:ErpTransferIntervalSeconds", 300);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<IErpTransferExecutor>();

                    await executor.ExecutePendingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERP transfer worker hata verdi.");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }
}