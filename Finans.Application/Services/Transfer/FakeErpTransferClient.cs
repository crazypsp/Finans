using Finans.Application.Abstractions.Transfer;
using Finans.Contracts.Transfer;

namespace Finans.Application.Services.Transfer
{
    /// <summary>
    /// Neden var?
    /// - ERP executor akışını gerçek Logo bağımlılığı olmadan test etmek için.
    /// - Gün 5 sonunda sistemin kuyruk/log/status yapısı doğrulanır.
    /// - Sonraki adımda bu sınıf, LogoTigerErpTransferClient ile değiştirilecek.
    /// </summary>
    public sealed class FakeErpTransferClient : IErpTransferClient
    {
        public Task<ErpTransferExecutionResultDto> TransferAsync(
            int companyId,
            int bankTransactionId,
            string? currentCode,
            string? glCode,
            string? bankAccountCode,
            CancellationToken ct = default)
        {
            var result = new ErpTransferExecutionResultDto
            {
                IsSuccess = true,
                VoucherNo = $"FAKE-VCH-{bankTransactionId}",
                ReferenceNo = $"FAKE-REF-{companyId}-{bankTransactionId}",
                Message = "Fake ERP transfer başarılı."
            };

            return Task.FromResult(result);
        }
    }
}