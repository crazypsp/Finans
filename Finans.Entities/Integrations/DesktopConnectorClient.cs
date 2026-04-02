using Finans.Entities.Common;

namespace Finans.Entities.Integration
{
    /// <summary>
    /// Firma bazında kurulu connector istemcisini temsil eder.
    /// Hangi makinede, hangi versiyonla, ne zaman heartbeat attığı burada tutulur.
    /// </summary>
    public sealed class DesktopConnectorClient : AuditableEntity
    {
        public int CompanyId { get; set; }

        public string MachineName { get; set; } = null!;
        public string ConnectorKey { get; set; } = null!;
        public string Version { get; set; } = null!;

        public bool IsActive { get; set; }
        public bool IsLicensed { get; set; }

        public DateTime? LastHeartbeatAtUtc { get; set; }
        public DateTime? LastTransferAtUtc { get; set; }

        public string? LastStatus { get; set; }
        public string? LastError { get; set; }
    }
}