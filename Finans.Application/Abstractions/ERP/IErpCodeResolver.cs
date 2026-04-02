using Finans.Entities.Banking;

namespace Finans.Application.Abstractions.ERP
{
    public interface IErpCodeResolver
    {
        Task<(string? CurrentCode, string? GlCode, string? BankAccountCode)> ResolveAsync(
            BankTransaction transaction,
            CancellationToken ct = default);
    }
}
