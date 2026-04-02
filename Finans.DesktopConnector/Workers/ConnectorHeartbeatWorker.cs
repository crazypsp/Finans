using Finans.Application.Abstractions.Integration;

namespace Finans.DesktopConnector.Workers
{
    public sealed class ConnectorHeartbeatWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectorHeartbeatWorker> _logger;

        public ConnectorHeartbeatWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ConnectorHeartbeatWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var companyId = _configuration.GetValue<int>("Connector:CompanyId");
            var machineName = _configuration["Connector:MachineName"] ?? Environment.MachineName;
            var connectorKey = _configuration["Connector:ConnectorKey"] ?? "DEFAULT-CONNECTOR";
            var version = _configuration["Connector:Version"] ?? "1.0.0";
            var intervalSeconds = _configuration.GetValue<int>("Connector:PollIntervalSeconds", 60);

            using (var scope = _serviceProvider.CreateScope())
            {
                var heartbeatService = scope.ServiceProvider.GetRequiredService<IConnectorHeartbeatService>();
                var policyService = scope.ServiceProvider.GetRequiredService<IConnectorPolicyService>();

                await heartbeatService.RegisterOrUpdateAsync(
                    companyId: companyId,
                    machineName: machineName,
                    connectorKey: connectorKey,
                    version: version,
                    ct: stoppingToken);

                var policy = await policyService.CanTransferAsync(
                    companyId: companyId,
                    machineName: machineName,
                    version: version,
                    ct: stoppingToken);

                if (!policy.IsAllowed)
                {
                    await heartbeatService.WriteHeartbeatAsync(
                        companyId: companyId,
                        machineName: machineName,
                        version: version,
                        status: "Blocked",
                        message: policy.Message,
                        ct: stoppingToken);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IConnectorHeartbeatService>();

                    await service.WriteHeartbeatAsync(
                        companyId: companyId,
                        machineName: machineName,
                        version: version,
                        status: "Alive",
                        message: "Connector çalışıyor.",
                        ct: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connector heartbeat hatası.");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
    }
}