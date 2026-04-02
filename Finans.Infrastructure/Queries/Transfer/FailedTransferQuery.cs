using Dapper;
using Finans.Contracts.Transfer;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Transfer
{
    public sealed class FailedTransferQuery : IFailedTransferQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public FailedTransferQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<FailedTransferItemDto>> ListAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT TOP 500
    i.Id,
    i.ErpTransferBatchId,
    b.BatchNo,
    i.BankTransactionId,
    i.Status,
    i.ResultMessage,
    i.CurrentCode,
    i.GlCode,
    i.BankAccCode,
    i.CreatedAtUtc,
    i.UpdatedAtUtc
FROM ErpTransferItems i
INNER JOIN ErpTransferBatches b ON b.Id = i.ErpTransferBatchId
WHERE i.CompanyId = @CompanyId
  AND i.IsDeleted = 0
  AND i.Status = 'Failed'
ORDER BY i.Id DESC;";

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<FailedTransferItemDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }
    }
}
