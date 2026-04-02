namespace Finans.Contracts.Transfer
{
    public sealed class ErpTransferExecutionResultDto
    {
        public bool IsSuccess { get; set; }
        public string? VoucherNo { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Message { get; set; }
    }
}