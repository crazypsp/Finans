using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class CompanyErpConnection : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int ErpSystemId { get; set; }

        // SQL Server bilgileri (Rapor/okuma vb. için)
        public string? Server { get; set; }
        public int? Port { get; set; }
        public string? DatabaseName { get; set; }
        public bool UseIntegratedSecurity { get; set; } = false;

        public string? DbUser { get; set; }
        public string? DbPasswordEncrypted { get; set; }

        // 9 ek alan
        public string? ExtendedProperty01 { get; set; }
        public string? ExtendedProperty02 { get; set; }
        public string? ExtendedProperty03 { get; set; }
        public string? ExtendedProperty04 { get; set; }
        public string? ExtendedProperty05 { get; set; }
        public string? ExtendedProperty06 { get; set; }
        public string? ExtendedProperty07 { get; set; }
        public string? ExtendedProperty08 { get; set; }
        public string? ExtendedProperty09 { get; set; }

        public string? ConnectionOptionsJson { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
