using Finans.Application.Abstractions.ERP;

namespace Finans.DesktopConnector.Workers
{
    public sealed class ErpAccountSyncWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ErpAccountSyncWorker> _logger;

        public ErpAccountSyncWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ErpAccountSyncWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalMinutes = _configuration.GetValue<int>("Worker:ErpAccountSyncIntervalMinutes", 60);
            var companyId = _configuration.GetValue<int>("Connector:CompanyId", 1);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IErpAccountSyncService>();

                    await syncService.SyncGlAccountsAsync(companyId, stoppingToken);
                    await syncService.SyncCurrentAccountsAsync(companyId, stoppingToken);
                    await syncService.SyncBankAccountsAsync(companyId, stoppingToken);

                    _logger.LogInformation("Logo Tiger hesap planı senkronizasyonu tamamlandı. CompanyId={CompanyId}", companyId);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Logo Tiger hesap planı senkronizasyon hatası.");
                }

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
        }
    }
}
