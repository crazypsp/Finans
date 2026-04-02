namespace Finans.Contracts.Transfer
{
    public sealed class FailedTransferItemDto
    {
        public int Id { get; set; }
        public int ErpTransferBatchId { get; set; }
        public string BatchNo { get; set; } = "";
        public int BankTransactionId { get; set; }

        public string Status { get; set; } = "";
        public string? ResultMessage { get; set; }

        public string? CurrentCode { get; set; }
        public string? GlCode { get; set; }
        public string? BankAccCode { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
