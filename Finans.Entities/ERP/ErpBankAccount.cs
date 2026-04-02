using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class ErpBankAccount : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int ErpSystemId { get; set; }

        public string BankAccCode { get; set; } = null!;
        public string BankAccName { get; set; } = null!;

        public string? Iban { get; set; }
        public string? Currency { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastSyncedAtUtc { get; set; }
    }
}
