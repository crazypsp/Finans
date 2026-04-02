using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Connector
{
    public class DesktopConnectorHeartbeat : AuditableEntity
    {
        public int DesktopConnectorInstallationId { get; set; }
        public DateTime HeartbeatAtUtc { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Running"; // Running/Idle/Error
        public string? MetricsJson { get; set; }
    }
}
