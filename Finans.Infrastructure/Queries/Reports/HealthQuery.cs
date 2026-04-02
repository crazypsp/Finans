using Dapper;
using Finans.Contracts.Reports;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Reports
{
    public sealed class HealthQuery : IHealthQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public HealthQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<SystemHealthSummaryDto> GetSummaryAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT
    CASE WHEN EXISTS (
        SELECT 1
        FROM DesktopConnectorClients
        WHERE CompanyId = @CompanyId
          AND IsDeleted = 0
          AND IsActive = 1
          AND IsLicensed = 1
    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasActiveConnector,

    CASE WHEN EXISTS (
        SELECT 1
        FROM BankCredentials
        WHERE CompanyId = @CompanyId
          AND IsDeleted = 0
          AND IsActive = 1
    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasActiveBankCredential,

    (SELECT COUNT(1)
     FROM ErpTransferItems
     WHERE CompanyId = @CompanyId
       AND IsDeleted = 0
       AND Status = 'Failed') AS FailedTransferCount,

    (SELECT COUNT(1)
     FROM ErpTransferItems
     WHERE CompanyId = @CompanyId
       AND IsDeleted = 0
       AND Status = 'Pending') AS PendingTransferCount,

    (SELECT MAX(LastHeartbeatAtUtc)
     FROM DesktopConnectorClients
     WHERE CompanyId = @CompanyId
       AND IsDeleted = 0) AS LastHeartbeatAtUtc,

    (SELECT MAX(ExecutedAtUtc)
     FROM BankApiPayloads
     WHERE CompanyId = @CompanyId
       AND IsDeleted = 0) AS LastBankImportAtUtc,

    (SELECT MAX(TransferredAtUtc)
     FROM ErpTransferItems
     WHERE CompanyId = @CompanyId
       AND IsDeleted = 0) AS LastErpTransferAtUtc;
";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstAsync<SystemHealthSummaryDto>(sql, new { CompanyId = companyId });
        }
    }
}