using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class ErpGlAccount : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int ErpSystemId { get; set; }

        public string GlCode { get; set; } = null!;
        public string GlName { get; set; } = null!;

        public bool IsActive { get; set; } = true;
        public DateTime? LastSyncedAtUtc { get; set; }
    }
}
