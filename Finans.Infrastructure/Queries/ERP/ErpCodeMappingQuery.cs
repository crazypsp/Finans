using Dapper;
using Finans.Contracts.ERP;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.ERP
{
    public sealed class ErpCodeMappingQuery : IErpCodeMappingQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public ErpCodeMappingQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<ErpCodeMappingDto>> ListAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT
    Id,
    CompanyId,
    BankId,
    Currency,
    DebitCredit,
    DescriptionKeyword,
    CurrentCode,
    GlCode,
    BankAccountCode,
    Priority,
    IsActive
FROM ErpCodeMappings
WHERE CompanyId = @CompanyId
  AND IsDeleted = 0
ORDER BY Priority ASC, Id DESC;";

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<ErpCodeMappingDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }
    }
}