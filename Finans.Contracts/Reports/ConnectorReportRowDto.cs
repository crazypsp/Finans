namespace Finans.Contracts.Reports
{
    public sealed class ConnectorReportRowDto
    {
        public string MachineName { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsLicensed { get; set; }
        public DateTime? LastHeartbeatAtUtc { get; set; }
        public DateTime? LastTransferAtUtc { get; set; }
        public string? LastStatus { get; set; }
        public string? LastError { get; set; }
    }
}