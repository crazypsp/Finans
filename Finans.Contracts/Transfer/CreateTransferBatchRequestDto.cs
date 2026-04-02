namespace Finans.Contracts.Transfer
{
    public sealed class CreateTransferBatchRequestDto
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }

        public List<int> BankTransactionIds { get; set; } = new();

        // ERP tarafı seçimleri
        public string? CurrentCode { get; set; }
        public string? GlCode { get; set; }
        public string? BankAccountCode { get; set; }

        public string TransferType { get; set; } = "MANUAL";
    }
}