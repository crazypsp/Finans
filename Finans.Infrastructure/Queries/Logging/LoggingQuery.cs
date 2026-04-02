using Dapper;
using Finans.Contracts.Logging;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Logging
{
    public sealed class LoggingQuery : ILoggingQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public LoggingQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT TOP 500
    Id,
    CompanyId,
    UserId,
    EntityName,
    ActionType,
    RecordId,
    Description,
    IpAddress,
    MachineName,
    OccurredAtUtc
FROM AuditLogs
WHERE CompanyId = @CompanyId
  AND IsDeleted = 0
ORDER BY Id DESC;";

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<AuditLogDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }

        public async Task<IReadOnlyList<SystemNotificationDto>> GetNotificationsAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT TOP 200
    Id,
    Title,
    Message,
    Level,
    IsRead,
    CreatedAtUtc,
    ReadAtUtc,
    Source,
    ReferenceId
FROM SystemNotifications
WHERE CompanyId = @CompanyId
  AND IsDeleted = 0
ORDER BY Id DESC;";

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<SystemNotificationDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }
    }
}
