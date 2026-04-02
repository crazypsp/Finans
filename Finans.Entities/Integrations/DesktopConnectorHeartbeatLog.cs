using Finans.Entities.Common;

namespace Finans.Entities.Integration
{
    /// <summary>
    /// Connector'ın periyodik heartbeat kayıtları.
    /// Operasyonel izleme ve sorun analizi için tutulur.
    /// </summary>
    public sealed class DesktopConnectorHeartbeatLog : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int DesktopConnectorClientId { get; set; }

        public string MachineName { get; set; } = null!;
        public string Version { get; set; } = null!;

        public DateTime HeartbeatAtUtc { get; set; }
        public string Status { get; set; } = "Alive";
        public string? Message { get; set; }
    }
}