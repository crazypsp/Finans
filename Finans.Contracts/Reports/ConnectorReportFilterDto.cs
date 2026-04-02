namespace Finans.Contracts.Reports
{
    public sealed class ConnectorReportFilterDto
    {
        public int CompanyId { get; set; }

        public string? MachineName { get; set; }
        public string? Version { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsLicensed { get; set; }

        public DateTime? LastHeartbeatStart { get; set; }
        public DateTime? LastHeartbeatEnd { get; set; }
    }
}
