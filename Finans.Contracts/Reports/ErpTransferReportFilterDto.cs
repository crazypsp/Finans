namespace Finans.Contracts.Reports
{
    public sealed class ErpTransferReportFilterDto
    {
        public int CompanyId { get; set; }

        public string? BatchNo { get; set; }
        public string? Status { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? MinTotalCount { get; set; }
        public int? MaxTotalCount { get; set; }
    }
}
