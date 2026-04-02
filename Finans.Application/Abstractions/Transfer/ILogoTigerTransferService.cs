using Finans.Contracts.Transfer;

namespace Finans.Application.Abstractions.Transfer
{
    public interface ILogoTigerTransferService
    {
        Task<ErpTransferExecutionResultDto> TransferBankTransactionAsync(
            int companyId,
            int bankTransactionId,
            string? currentCode,
            string? glCode,
            string? bankAccountCode,
            CancellationToken ct = default);
    }
}