namespace Finans.Application.Models.ERP
{
    public sealed class BankTransactionRuleMatchResult
    {
        public bool IsMatched { get; set; }

        public string? CurrentCode { get; set; }
        public string? GlCode { get; set; }
        public string? BankAccountCode { get; set; }

        public string? TransactionTag { get; set; }
        public string? DescriptionOverride { get; set; }
    }
}
