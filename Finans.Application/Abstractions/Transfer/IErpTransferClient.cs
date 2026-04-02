using Finans.Contracts.Transfer;

namespace Finans.Application.Abstractions.Transfer
{
    /// <summary>
    /// ERP sistemine tekil transfer yapan istemci.
    /// Bugün fake/mock implementasyon ile başlayacağız.
    /// Sonra Logo Tiger UnityObjects implementasyonu buraya takılacak.
    /// </summary>
    public interface IErpTransferClient
    {
        Task<ErpTransferExecutionResultDto> TransferAsync(
            int companyId,
            int bankTransactionId,
            string? currentCode,
            string? glCode,
            string? bankAccountCode,
            CancellationToken ct = default);
    }
}