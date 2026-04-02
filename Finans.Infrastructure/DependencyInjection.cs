using Finans.Infrastructure.Banking.DependencyInjection;
using Finans.Infrastructure.Data;
using Finans.Infrastructure.Queries.Dashboard;
using Finans.Infrastructure.Queries.ERP;
using Finans.Infrastructure.Queries.Integration;
using Finans.Infrastructure.Queries.Logging;
using Finans.Infrastructure.Queries.Reports;
using Finans.Infrastructure.Queries.Transfer;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

            services.AddScoped<IDashboardQuery, DashboardQuery>();
            services.AddScoped<ITransferQuery, TransferQuery>();
            services.AddScoped<IDesktopConnectorQuery, DesktopConnectorQuery>();
            services.AddScoped<IErpTransferBatchQuery, ErpTransferBatchQuery>();
            services.AddScoped<IReportQuery, ReportQuery>();
            services.AddScoped<IHealthQuery, HealthQuery>();
            services.AddScoped<IErpCodeMappingQuery, ErpCodeMappingQuery>();
            services.AddScoped<IFailedTransferQuery, FailedTransferQuery>();
            services.AddScoped<IBankTransactionRuleQuery, BankTransactionRuleQuery>();
            services.AddScoped<ILoggingQuery, LoggingQuery>();

            services.AddBankingInfrastructure();

            return services;
        }
    }
}