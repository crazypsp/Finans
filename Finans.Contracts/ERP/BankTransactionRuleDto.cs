namespace Finans.Contracts.ERP
{
    public sealed class BankTransactionRuleDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        public int? BankId { get; set; }
        public string? AccountNumber { get; set; }

        public string? Currency { get; set; }
        public string? DebitCredit { get; set; }

        public string? DescriptionContains { get; set; }

        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public string? CurrentCode { get; set; }
        public string? GlCode { get; set; }
        public string? BankAccountCode { get; set; }

        public string? TransactionTag { get; set; }
        public string? DescriptionOverride { get; set; }

        public int Priority { get; set; }
        public bool IsActive { get; set; }
    }
}
