namespace Finans.Contracts.Reports
{
    public sealed class ErpTransferReportRowDto
    {
        public string BatchNo { get; set; } = "";
        public string Status { get; set; } = "";
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}