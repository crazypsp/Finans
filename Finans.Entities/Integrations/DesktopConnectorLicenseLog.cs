using Finans.Entities.Common;

namespace Finans.Entities.Integration
{
    /// <summary>
    /// Lisans doğrulama veya sürüm uyuşmazlığı logları.
    /// </summary>
    public sealed class DesktopConnectorLicenseLog : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int? DesktopConnectorClientId { get; set; }

        public string MachineName { get; set; } = null!;
        public string Version { get; set; } = null!;

        public string Status { get; set; } = null!;
        public string? Message { get; set; }

        public DateTime CheckedAtUtc { get; set; }
    }
}