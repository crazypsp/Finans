using Finans.Contracts.Logging;

namespace Finans.Infrastructure.Queries.Logging
{
    public interface ILoggingQuery
    {
        Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(int companyId, CancellationToken ct);
        Task<IReadOnlyList<SystemNotificationDto>> GetNotificationsAsync(int companyId, CancellationToken ct);
    }
}
