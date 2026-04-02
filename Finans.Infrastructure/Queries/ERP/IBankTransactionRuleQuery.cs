using Finans.Contracts.ERP;

namespace Finans.Infrastructure.Queries.ERP
{
    public interface IBankTransactionRuleQuery
    {
        Task<IReadOnlyList<BankTransactionRuleDto>> ListAsync(int companyId, CancellationToken ct);
    }
}
