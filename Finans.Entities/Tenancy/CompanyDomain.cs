using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Tenancy
{
    public class CompanyDomain : AuditableEntity
    {
        public int CompanyId { get; set; }
        public string Domain { get; set; } = null!;

        public bool IsPrimary { get; set; } = false;
        public bool IsVerified { get; set; } = false;
    }
}
