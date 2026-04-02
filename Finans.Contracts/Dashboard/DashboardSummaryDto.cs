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

        public DateTime? LastImportAtUtc { get; set; }
        public int FailedImportCountLast24h { get; set; }
    }
}
