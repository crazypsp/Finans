using Finans.Application.Abstractions.Banking;
using Finans.Application.Abstractions.ERP;
using Finans.Application.Abstractions.Identity;
using Finans.Application.Abstractions.Integration;
using Finans.Application.Abstractions.Logging;
using Finans.Application.Abstractions.Security;
using Finans.Application.Abstractions.Transfer;
using Finans.Application.Services.Banking;
using Finans.Application.Services.ERP;
using Finans.Application.Services.Identity;
using Finans.Application.Services.Integration;
using Finans.Application.Services.Logging;
using Finans.Application.Services.Security;
using Finans.Application.Services.Transfer;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Application
{
    /// <summary>
    /// Neden var?
    /// - Web/Api/Worker aynı servis kayıtlarını tekrar tekrar yazmasın diye.
    /// - Host projelerinde tek satırla Application servisleri eklenir.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ITransferCommandService, TransferCommandService>();
            services.AddScoped<IBankImportService, BankImportService>();
            services.AddScoped<ITransferBatchService, TransferBatchService>();
            services.AddScoped<IErpTransferClient, FakeErpTransferClient>();
            services.AddScoped<IErpTransferExecutor, ErpTransferExecutor>();
            services.AddScoped<IConnectorHeartbeatService, ConnectorHeartbeatService>();
            services.AddScoped<IConnectorPolicyService, ConnectorPolicyService>();
            services.AddScoped<IBankImportValidationService, BankImportValidationService>();
            services.AddScoped<ITransferRetryService, TransferRetryService>();
            services.AddScoped<IErpCodeMappingService, ErpCodeMappingService>();
            services.AddScoped<IErpCodeResolver, ErpCodeResolver>();
            services.AddScoped<IBankTransactionRuleService, BankTransactionRuleService>();
            services.AddScoped<IBankTransactionRuleResolver, BankTransactionRuleResolver>();
            services.AddScoped<ICompanyAuthorizationService, CompanyAuthorizationService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IErpAccountSyncService, ErpAccountSyncService>();
            services.AddScoped<IBankTransactionMatchingService, BankTransactionMatchingService>();
            return services;
        }
    }
}