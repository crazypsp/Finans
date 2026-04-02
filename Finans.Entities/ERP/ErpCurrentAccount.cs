using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class ErpCurrentAccount : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int ErpSystemId { get; set; }

        public string CurrentCode { get; set; } = null!;
        public string CurrentName { get; set; } = null!;

        public string? TaxNumber { get; set; }
        public string? IdentityNumber { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastSyncedAtUtc { get; set; }
    }
}
