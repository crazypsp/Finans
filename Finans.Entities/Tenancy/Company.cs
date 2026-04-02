using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Tenancy
{
    public class Company : AuditableEntity
    {
        public string Name { get; set; } = null!;

        public CompanyType CompanyType { get; set; }
        public int? ParentCompanyId { get; set; }

        // Tek domain (esas kullanım)
        public string PrimaryDomain { get; set; } = null!;

        // Ülke/dil
        public string CountryIso2 { get; set; } = null!;        // TR, DE...
        public string DefaultCultureName { get; set; } = null!; // tr-TR
        public string TimeZoneId { get; set; } = null!;         // Europe/Istanbul

        public bool IsActive { get; set; } = true;
    }
}
