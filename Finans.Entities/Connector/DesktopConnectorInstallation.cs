using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Connector
{
    public class DesktopConnectorInstallation : AuditableEntity
    {
        public int CompanyId { get; set; }

        public string MachineId { get; set; } = null!;
        public string? MachineName { get; set; }
        public string? WindowsUserName { get; set; }

        public string? InstalledVersion { get; set; }
        public string? CurrentVersion { get; set; }

        public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastStartAtUtc { get; set; }
        public DateTime? LastStopAtUtc { get; set; }

        public int? ErpSystemId { get; set; } // Logo
        public bool IsActive { get; set; } = true;

        public string? SettingsJson { get; set; }
    }
}
