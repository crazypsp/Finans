using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class ErpSystem : AuditableEntity
    {
        public string Name { get; set; } = null!; // Logo Tiger
        public string Code { get; set; } = null!; // LOGO_TIGER
        public bool IsActive { get; set; } = true;
    }
}
