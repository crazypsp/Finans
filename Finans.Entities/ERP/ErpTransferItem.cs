using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class ErpTransferItem : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int ErpTransferBatchId { get; set; }
        public int BankTransactionId { get; set; }

        // Kullanıcı seçimleri / aktarım parametreleri
        public string? CurrentCode { get; set; }
        public string? GlCode { get; set; }
        public string? BankAccCode { get; set; }

        public string Status { get; set; } = "Pending"; // Pending/Success/Failed
        public string? VoucherNo { get; set; }
        public string? ResultMessage { get; set; }

        public DateTime? TransferredAtUtc { get; set; }
    }
}
