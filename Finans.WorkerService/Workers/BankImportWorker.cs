using Finans.Application.Abstractions.Banking;

namespace Finans.WorkerService.Workers
{
    public sealed class BankImportWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BankImportWorker> _logger;
        private readonly IConfiguration _configuration;

        public BankImportWorker(
            IServiceProvider serviceProvider,
            ILogger<BankImportWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _configuration.GetValue<int>("Worker:BankImportIntervalSeconds", 300);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var importService = scope.ServiceProvider.GetRequiredService<IBankImportService>();

                    await importService.RunImportAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bank import worker hata verdi.");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }
}