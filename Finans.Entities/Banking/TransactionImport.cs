using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Banking
{
    public class TransactionImport : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public int BankId { get; set; }

        public string? AccountNumber { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int TotalRecords { get; set; }
        public int ImportedRecords { get; set; }

        public TransferStatus Status { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }

        public string? RequestParametersJson { get; set; }
        public int? BankApiPayloadId { get; set; }
    }
}
