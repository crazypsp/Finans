using System.Text;
using Dapper;
using Finans.Contracts.Reports;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Reports
{
    public sealed class ReportQuery : IReportQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public ReportQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT
    (SELECT COUNT(1) FROM BankTransactions WHERE CompanyId = @CompanyId AND IsDeleted = 0) AS TotalBankTransactionCount,
    (SELECT COUNT(1) FROM ErpTransferItems WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND Status = 'Pending') AS PendingTransferCount,
    (SELECT COUNT(1) FROM ErpTransferItems WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND Status = 'Success') AS SuccessTransferCount,
    (SELECT COUNT(1) FROM ErpTransferItems WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND Status = 'Failed') AS FailedTransferCount,
    (SELECT COUNT(1) FROM DesktopConnectorClients WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1) AS ActiveConnectorCount,
    (SELECT COUNT(1) FROM Banks WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1) AS ActiveBankCount;
";
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstAsync<DashboardSummaryDto>(sql, new { CompanyId = companyId });
        }

        public async Task<IReadOnlyList<BankImportReportRowDto>> GetBankImportReportAsync(
            BankImportReportFilterDto filter,
            CancellationToken ct)
        {
            var sql = new StringBuilder(@"
SELECT TOP 500
    p.BankId,
    b.BankName,
    p.ExecutedAtUtc,
    p.IsSuccess,
    p.ErrorMessage,
    p.Operation
FROM BankApiPayloads p
INNER JOIN Banks b ON b.Id = p.BankId
WHERE p.CompanyId = @CompanyId
  AND p.IsDeleted = 0
");

            if (filter.BankId.HasValue)
                sql.AppendLine("AND p.BankId = @BankId");

            if (filter.IsSuccess.HasValue)
                sql.AppendLine("AND p.IsSuccess = @IsSuccess");

            if (filter.StartDate.HasValue)
                sql.AppendLine("AND p.ExecutedAtUtc >= @StartDate");

            if (filter.EndDate.HasValue)
                sql.AppendLine("AND p.ExecutedAtUtc <= @EndDate");

            if (!string.IsNullOrWhiteSpace(filter.Operation))
                sql.AppendLine("AND p.Operation = @Operation");

            if (!string.IsNullOrWhiteSpace(filter.ErrorContains))
                sql.AppendLine("AND p.ErrorMessage LIKE '%' + @ErrorContains + '%'");

            sql.AppendLine("ORDER BY p.Id DESC;");

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<BankImportReportRowDto>(sql.ToString(), filter);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<ErpTransferReportRowDto>> GetErpTransferReportAsync(
            ErpTransferReportFilterDto filter,
            CancellationToken ct)
        {
            var sql = new StringBuilder(@"
SELECT TOP 500
    BatchNo,
    Status,
    TotalCount,
    SuccessCount,
    FailedCount,
    StartedAtUtc,
    CompletedAtUtc
FROM ErpTransferBatches
WHERE CompanyId = @CompanyId
  AND IsDeleted = 0
");

            if (!string.IsNullOrWhiteSpace(filter.BatchNo))
                sql.AppendLine("AND BatchNo LIKE '%' + @BatchNo + '%'");

            if (!string.IsNullOrWhiteSpace(filter.Status))
                sql.AppendLine("AND Status = @Status");

            if (filter.StartDate.HasValue)
                sql.AppendLine("AND StartedAtUtc >= @StartDate");

            if (filter.EndDate.HasValue)
                sql.AppendLine("AND StartedAtUtc <= @EndDate");

            if (filter.MinTotalCount.HasValue)
                sql.AppendLine("AND TotalCount >= @MinTotalCount");

            if (filter.MaxTotalCount.HasValue)
                sql.AppendLine("AND TotalCount <= @MaxTotalCount");

            sql.AppendLine("ORDER BY Id DESC;");

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<ErpTransferReportRowDto>(sql.ToString(), filter);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<ConnectorReportRowDto>> GetConnectorReportAsync(
            ConnectorReportFilterDto filter,
            CancellationToken ct)
        {
            var sql = new StringBuilder(@"
SELECT TOP 500
    MachineName,
    Version,
    IsActive,
    IsLicensed,
    LastHeartbeatAtUtc,
    LastTransferAtUtc,
    LastStatus,
    LastError
FROM DesktopConnectorClients
WHERE CompanyId = @CompanyId
  AND IsDeleted = 0
");

            if (!string.IsNullOrWhiteSpace(filter.MachineName))
                sql.AppendLine("AND MachineName LIKE '%' + @MachineName + '%'");

            if (!string.IsNullOrWhiteSpace(filter.Version))
                sql.AppendLine("AND Version = @Version");

            if (filter.IsActive.HasValue)
                sql.AppendLine("AND IsActive = @IsActive");

            if (filter.IsLicensed.HasValue)
                sql.AppendLine("AND IsLicensed = @IsLicensed");

            if (filter.LastHeartbeatStart.HasValue)
                sql.AppendLine("AND LastHeartbeatAtUtc >= @LastHeartbeatStart");

            if (filter.LastHeartbeatEnd.HasValue)
                sql.AppendLine("AND LastHeartbeatAtUtc <= @LastHeartbeatEnd");

            sql.AppendLine("ORDER BY Id DESC;");

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<ConnectorReportRowDto>(sql.ToString(), filter);
            return rows.ToList();
        }
    }
}
