namespace Finans.Contracts.Reports
{
    public sealed class BankImportReportRowDto
    {
        public int BankId { get; set; }
        public string BankName { get; set; } = "";
        public DateTime ExecutedAtUtc { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string Operation { get; set; } = "";
    }
}