using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Contracts.Dashboard
{
    /// <summary>
    /// Neden var?
    /// - Dashboard ekranı için tek query ile dönecek özet metrik modeli.
    /// - UI katmanı DB entity'lerini taşımasın, sadece ihtiyacı olan metrikleri alsın.
    /// </summary>
    public sealed class DashboardSummaryDto
    {
        public int TotalTransactions { get; set; }
        public int NotTransferredCount { get; set; }
        public int TransferredCount { get; set; }

        public decimal TotalAmountInRange { get; set; }
        public decimal CashInAmount { get; set; }
        public decimal CashOutAmount { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal LatestBalance { get; set; }

        public int PendingTransferCount { get; set; }
        public int FailedTransferCount { get; set; }
        public int ActiveBankCount { get; set; }
        public int ActiveBankAccountCount { get; set; }

        public int ErpGlAccountCount { get; set; }
        public int ErpCurrentAccountCount { get; set; }
        public int ErpBankAccountCount { get; set; }

        public DateTime? LastImportAtUtc { get; set; }
        public DateTime? LastErpSyncAtUtc { get; set; }
        public int FailedImportCountLast24h { get; set; }
    }
}
