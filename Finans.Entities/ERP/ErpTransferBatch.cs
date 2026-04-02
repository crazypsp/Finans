using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class ErpTransferBatch : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public int ErpSystemId { get; set; }

        public string BatchNo { get; set; } = null!;      // UI'da göster
        public string TransferType { get; set; } = null!; // SINGLE / BULK
        public string? FilterJson { get; set; }           // seçilen filtreler

        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }

        public string Status { get; set; } = "Pending";   // Running/Done/Partial/Failed
    }
}
