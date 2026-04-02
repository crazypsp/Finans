using Finans.Application.Models.ERP;
using Finans.Entities.Banking;

namespace Finans.Application.Abstractions.ERP
{
    public interface IBankTransactionRuleResolver
    {
        Task<BankTransactionRuleMatchResult> ResolveAsync(BankTransaction transaction, CancellationToken ct = default);
    }
}
