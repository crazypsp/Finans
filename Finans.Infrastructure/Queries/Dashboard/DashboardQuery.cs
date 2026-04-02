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
    (SELECT COUNT(1) FROM BankTransactions WITH (NOLOCK) WHERE CompanyId = @CompanyId) AS TotalTransactions,
    (SELECT COUNT(1) FROM BankTransactions WITH (NOLOCK) WHERE CompanyId = @CompanyId AND IsTransferred = 0) AS NotTransferredCount,
    (SELECT COUNT(1) FROM BankTransactions WITH (NOLOCK) WHERE CompanyId = @CompanyId AND IsTransferred = 1) AS TransferredCount,
    (SELECT ISNULL(SUM(Amount),0) FROM BankTransactions WITH (NOLOCK)
        WHERE CompanyId = @CompanyId
          AND (@Start IS NULL OR TransactionDate >= @Start)
          AND (@End IS NULL OR TransactionDate <= @End)
    ) AS TotalAmountInRange,
    (SELECT MAX(CompletedAtUtc) FROM TransactionImports WITH (NOLOCK) WHERE CompanyId = @CompanyId) AS LastImportAtUtc,
    (SELECT COUNT(1) FROM TransactionImports WITH (NOLOCK)
        WHERE CompanyId = @CompanyId
          AND Status IN (1,4,5) -- Warning/Error/Critical gibi değil: biz TransferStatus kullanıyoruz, bunu Gün 2 sonunda netleştiririz
          AND StartedAtUtc >= DATEADD(HOUR,-24, SYSUTCDATETIME())
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