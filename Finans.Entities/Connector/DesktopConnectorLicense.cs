using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Connector
{
    public class DesktopConnectorLicense : AuditableEntity
    {
        public int CompanyId { get; set; }

        public string LicenseKey { get; set; } = null!;
        public DateTime ValidFromUtc { get; set; }
        public DateTime ValidToUtc { get; set; }

        public int MaxDeviceCount { get; set; } = 1;
        public bool IsRevoked { get; set; } = false;

        public string LicenseType { get; set; } = "Paid"; // Trial/Paid/Enterprise
        public string? Notes { get; set; }
    }
}
