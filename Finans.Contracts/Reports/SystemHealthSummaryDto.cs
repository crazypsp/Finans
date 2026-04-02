namespace Finans.Contracts.Reports
{
    public sealed class SystemHealthSummaryDto
    {
        public bool HasActiveConnector { get; set; }
        public bool HasActiveBankCredential { get; set; }
        public int FailedTransferCount { get; set; }
        public int PendingTransferCount { get; set; }
        public DateTime? LastHeartbeatAtUtc { get; set; }
        public DateTime? LastBankImportAtUtc { get; set; }
        public DateTime? LastErpTransferAtUtc { get; set; }
    }
}