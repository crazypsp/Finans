using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Tenancy
{
    public class CompanyUser : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }

        // Kullanıcı özel dil (boşsa Company.DefaultCultureName)
        public string? PreferredCultureName { get; set; }

        public bool IsCompanyAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
