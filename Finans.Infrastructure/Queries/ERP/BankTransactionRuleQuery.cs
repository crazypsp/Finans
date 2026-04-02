using Dapper;
using Finans.Contracts.ERP;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.ERP
{
    public sealed class BankTransactionRuleQuery : IBankTransactionRuleQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public BankTransactionRuleQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<BankTransactionRuleDto>> ListAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT
    Id,
    CompanyId,
    BankId,
    AccountNumber,
    Currency,
    DebitCredit,
    DescriptionContains,
    MinAmount,
    MaxAmount,
    CurrentCode,
    GlCode,
    BankAccountCode,
    TransactionTag,
    DescriptionOverride,
    Priority,
    IsActive
FROM BankTransactionRules
WHERE CompanyId = @CompanyId
  AND IsDeleted = 0
ORDER BY Priority ASC, Id DESC;";

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<BankTransactionRuleDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }
    }
}
