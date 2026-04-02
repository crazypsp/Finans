using Finans.Contracts.ERP;

namespace Finans.Application.Abstractions.ERP
{
    public interface IBankTransactionRuleService
    {
        Task CreateOrUpdateAsync(BankTransactionRuleDto dto, CancellationToken ct = default);
        Task DeleteAsync(int companyId, int id, CancellationToken ct = default);
    }
}
