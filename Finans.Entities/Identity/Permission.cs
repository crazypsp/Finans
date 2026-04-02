using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Identity
{
    public class Permission : AuditableEntity
    {
        public string Code { get; set; } = null!;  // BANK_IMPORT_RUN, ERP_TRANSFER_RUN...
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
