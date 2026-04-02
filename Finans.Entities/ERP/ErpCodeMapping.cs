using Finans.Entities.Common;

namespace Finans.Entities.ERP
{
    public sealed class ErpCodeMapping : AuditableEntity
    {
        public int CompanyId { get; set; }

        public int? BankId { get; set; }

        public string? Currency { get; set; }
        public string? DebitCredit { get; set; }

        public string? DescriptionKeyword { get; set; }

        public string? CurrentCode { get; set; }
        public string? GlCode { get; set; }
        public string? BankAccountCode { get; set; }

        public int Priority { get; set; } = 0;
        public bool IsActive { get; set; }
    }
}