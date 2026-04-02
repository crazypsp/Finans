namespace Finans.Contracts.Reports
{
    public sealed class BankImportReportFilterDto
    {
        public int CompanyId { get; set; }

        public int? BankId { get; set; }
        public bool? IsSuccess { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? Operation { get; set; }
        public string? ErrorContains { get; set; }
    }
}
