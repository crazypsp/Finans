using Dapper;
using Finans.Contracts.Transfer;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Transfer
{
    public sealed class ErpTransferBatchQuery : IErpTransferBatchQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public ErpTransferBatchQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<ErpTransferBatchListDto>> ListAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT
    Id,
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
ORDER BY Id DESC;";

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<ErpTransferBatchListDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }
    }
}