using Finans.Application.Models.Banking;

namespace Finans.Application.Abstractions.Banking
{
    /// <summary>
    /// Neden var?
    /// - Banka entegrasyonlarının ortak davranış sözleşmesi.
    /// - Application katmanı "ne yapılacak"ı tanımlar, Infrastructure "nasıl"ını uygular.
    /// </summary>
    public interface IBankProvider
    {
        string ProviderCode { get; }
        IReadOnlyCollection<string> ProviderAliases => Array.Empty<string>();
        Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default);
    }
}
