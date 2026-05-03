using Dapper;
using Finans.Contracts.Dashboard;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Dashboard
{
    /// <summary>
    /// Neden Dapper?
    /// - Dashboard gibi salt okuma ekranlarında EF tracking gereksiz maliyet.
    /// - Tek SQL ile hızlı özet metrik almak daha performanslı.
    /// </summary>
    public sealed class DashboardQuery : IDashboardQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public DashboardQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(int companyId, DateTime? start, DateTime? end, CancellationToken ct)
        {
            const string sql = @"
SELECT
    (SELECT COUNT(1) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS TotalTransactions,
    (SELECT COUNT(1) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsTransferred = 0
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS NotTransferredCount,
    (SELECT COUNT(1) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsTransferred = 1
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS TransferredCount,
    (SELECT ISNULL(SUM(Amount),0) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId
          AND IsDeleted = 0
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS TotalAmountInRange,
    (SELECT ISNULL(SUM(Amount),0) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND DebitCredit = 'C'
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS CashInAmount,
    (SELECT ISNULL(SUM(Amount),0) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND DebitCredit = 'D'
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS CashOutAmount,
    (SELECT
        ISNULL(SUM(CASE WHEN DebitCredit = 'C' THEN Amount ELSE -Amount END),0)
        FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS NetCashFlow,
    (SELECT TOP (1) ISNULL(BalanceAfterTransaction,0) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND BalanceAfterTransaction IS NOT NULL
        ORDER BY TransactionDate DESC, Id DESC
    ) AS LatestBalance,
    (SELECT COUNT(1) FROM ErpTransferItems WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND Status = 'Pending'
    ) AS PendingTransferCount,
    (SELECT COUNT(1) FROM ErpTransferItems WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND Status = 'Failed'
    ) AS FailedTransferCount,
    (SELECT COUNT(1) FROM Banks WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1
    ) AS ActiveBankCount,
    (SELECT COUNT(1) FROM BankAccounts WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1
    ) AS ActiveBankAccountCount,
    (SELECT COUNT(1) FROM ErpGlAccounts WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1
    ) AS ErpGlAccountCount,
    (SELECT COUNT(1) FROM ErpCurrentAccounts WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1
    ) AS ErpCurrentAccountCount,
    (SELECT COUNT(1) FROM ErpBankAccounts WITH (NOLOCK)
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1
    ) AS ErpBankAccountCount,
    (SELECT MAX(v) FROM (VALUES
        ((SELECT MAX(LastSyncedAtUtc) FROM ErpGlAccounts WITH (NOLOCK) WHERE CompanyId = @CompanyId)),
        ((SELECT MAX(LastSyncedAtUtc) FROM ErpCurrentAccounts WITH (NOLOCK) WHERE CompanyId = @CompanyId)),
        ((SELECT MAX(LastSyncedAtUtc) FROM ErpBankAccounts WITH (NOLOCK) WHERE CompanyId = @CompanyId))
    ) AS syncs(v)) AS LastErpSyncAtUtc,
    (SELECT MAX(ExecutedAtUtc) FROM BankApiPayloads WITH (NOLOCK) WHERE CompanyId = @CompanyId) AS LastImportAtUtc,
    (SELECT COUNT(1) FROM BankIntegrationLogs WITH (NOLOCK)
        WHERE CompanyId = @CompanyId
          AND Status IN ('Failed','Exception','ValidationFailed')
          AND OccurredAtUtc >= DATEADD(HOUR,-24, SYSUTCDATETIME())
    ) AS FailedImportCountLast24h;
";

            using var conn = _factory.CreateConnection();
            var dto = await conn.QuerySingleAsync<DashboardSummaryDto>(sql, new
            {
                CompanyId = companyId,
                Start = start,
                End = end
            });
            return dto;
        }
    }
}
