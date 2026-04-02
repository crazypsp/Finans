using Finans.Application.Abstractions.Transfer;
using Finans.Contracts.Transfer;

namespace Finans.DesktopConnector.Services
{
    public sealed class LogoTigerErpTransferClient : IErpTransferClient
    {
        private readonly ILogoTigerTransferService _logoTigerTransferService;

        public LogoTigerErpTransferClient(ILogoTigerTransferService logoTigerTransferService)
        {
            _logoTigerTransferService = logoTigerTransferService;
        }

        public Task<ErpTransferExecutionResultDto> TransferAsync(
            int companyId,
            int bankTransactionId,
            string? currentCode,
            string? glCode,
            string? bankAccountCode,
            CancellationToken ct = default)
        {
            return _logoTigerTransferService.TransferBankTransactionAsync(
                companyId,
                bankTransactionId,
                currentCode,
                glCode,
                bankAccountCode,
                ct);
        }
    }
}