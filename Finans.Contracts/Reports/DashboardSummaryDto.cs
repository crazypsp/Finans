namespace Finans.Contracts.Reports
{
    public sealed class DashboardSummaryDto
    {
        public int TotalBankTransactionCount { get; set; }
        public int PendingTransferCount { get; set; }
        public int SuccessTransferCount { get; set; }
        public int FailedTransferCount { get; set; }
        public int ActiveConnectorCount { get; set; }
        public int ActiveBankCount { get; set; }
    }
}